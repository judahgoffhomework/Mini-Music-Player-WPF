using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MiniPlayerWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MusicLib musicLib;

        private DataSet musicDataSet;
        private MediaPlayer mediaPlayer;
        private ObservableCollection<string> songIds;

        // Custom commands
        public static RoutedCommand Add = new RoutedCommand();
        public static RoutedCommand Update = new RoutedCommand();


        public MainWindow()
        {
            InitializeComponent();

            musicLib = new MusicLib();

            mediaPlayer = new MediaPlayer();

            try
            {
                musicDataSet = new DataSet();
                musicDataSet.ReadXmlSchema("music.xsd");
                musicDataSet.ReadXml("music.xml");
            }
            catch (Exception e)
            {
                DisplayError("Error loading file: " + e.Message);
            }

            PrintAllTables();

            Console.WriteLine("Total songs = " + musicDataSet.Tables["song"].Rows.Count);

            // Get a list of all song IDs
            DataTable songs = musicDataSet.Tables["song"];
            var ids = from row in songs.AsEnumerable()
                      orderby row["id"]
                      select row["id"].ToString();

            // Put the ids in an ObservableCollection, which has Add & Remove methods for use later.
            // The UI will update itself automatically if any changes are made to this collection.
            songIds = new ObservableCollection<string>(ids);     

            // Bind the song IDs to the combo box
            songIdComboBox.ItemsSource = songIds;
            
            // Select the first item
            if (songIdComboBox.Items.Count > 0)
            {
                songIdComboBox.SelectedItem = songIdComboBox.Items[0];
            }
        }

        private void PrintAllTables()
        {
            foreach (DataTable table in musicDataSet.Tables)
            {
                Console.WriteLine("Table name = " + table.TableName);
                foreach (DataRow row in table.Rows)
                {
                    Console.WriteLine("Row:");
                    int i = 0;
                    foreach (Object item in row.ItemArray)
                    {
                        Console.WriteLine(" " + table.Columns[i].Caption + "=" + item);
                        i++;
                    }
                }
                Console.WriteLine();
            }
        }
        
        private Song GetSongDetails(string filename)
        {
            Song song = null;

            try
            {
                // PM> Install-Package taglib
                // http://stackoverflow.com/questions/1750464/how-to-read-and-write-id3-tags-to-an-mp3-in-c
                TagLib.File file = TagLib.File.Create(filename);

                song = new Song
                {
                    Title = file.Tag.Title,
                    Artist = file.Tag.AlbumArtists.Length > 0 ? file.Tag.AlbumArtists[0] : "",
                    Album = file.Tag.Album,
                    Genre = file.Tag.Genres.Length > 0 ? file.Tag.Genres[0] : "",
                    Length = file.Properties.Duration.Minutes + ":" + file.Properties.Duration.Seconds,
                    Filename = filename
                };

                return song;
            }
            catch (TagLib.UnsupportedFormatException)
            {
                DisplayError("You did not select a valid song file.");
            }
            catch (Exception ex)
            {
                DisplayError(ex.Message);
            }

            return song;
        }

        private void DisplayError(string errorMessage)
        {
            MessageBox.Show(errorMessage, "MiniPlayer", MessageBoxButton.OK, MessageBoxImage.Error);
        }         

        private void showDataButton_Click(object sender, RoutedEventArgs e)
        {
            PrintAllTables();
        }
        
        private void songIdComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Display the selected song
            if (songIdComboBox.SelectedItem != null)
            {
                Console.WriteLine("Load song " + songIdComboBox.SelectedItem);
                int songId = Convert.ToInt32(songIdComboBox.SelectedItem);
                DataTable table = musicDataSet.Tables["song"];

                // Only one row should be selected
                foreach (DataRow row in table.Select("id=" + songId))
                {
                    titleTextBox.Text = row["title"].ToString();
                    artistTextBox.Text = row["artist"].ToString();
                    albumTextBox.Text = row["album"].ToString();
                    genreTextBox.Text = row["genre"].ToString();
                    lengthTextBox.Text = row["length"].ToString();
                    filenameTextBox.Text = row["filename"].ToString();                    
                }
            }
        }

        private void OpenCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.FileName = "";
            openFileDialog.DefaultExt = "*.wma;*.wav;*mp3";
            openFileDialog.Filter = "Media files|*.mp3;*.m4a;*.wma;*.wav|MP3 (*.mp3)|*.mp3|M4A (*.m4a)|*.m4a|Windows Media Audio (*.wma)|*.wma|Wave files (*.wav)|*.wav|All files|*.*";

            // Show open file dialog box
            bool? result = openFileDialog.ShowDialog();

            // Load the selected song
            if (result == true)
            {
                songIdComboBox.IsEnabled = false;
                Song s = GetSongDetails(openFileDialog.FileName);
                if (s != null)
                {
                    titleTextBox.Text = s.Title;
                    artistTextBox.Text = s.Artist;
                    albumTextBox.Text = s.Album;
                    genreTextBox.Text = s.Genre;
                    lengthTextBox.Text = s.Length;
                    filenameTextBox.Text = s.Filename;
                    mediaPlayer.Open(new Uri(s.Filename));
                    addButton.IsEnabled = true;
                }
            }
        }

        private void AddCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Console.WriteLine("Adding song");

            Song song = new Song
            {
                Title = titleTextBox.Text,
                Artist = artistTextBox.Text,
                Album = albumTextBox.Text,
                Filename = filenameTextBox.Text,
                Length = lengthTextBox.Text,
                Genre = genreTextBox.Text
            };
                      
            musicLib.AddSong(song);

            // Now that the id has been set, add it songIds, which automatically adds to the combo box
            songIdComboBox.IsEnabled = true;
            string id = song.Id.ToString();
            songIds.Add(id);
            songIdComboBox.SelectedIndex = songIdComboBox.Items.Count - 1;

            // There is at least one song that can be deleted
            deleteButton.IsEnabled = true;
        }

        private void UpdateCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string songId = songIdComboBox.SelectedItem.ToString();
            Console.WriteLine("Updating song " + songId);

            DataTable table = musicDataSet.Tables["song"];

            // Only one row should be selected
            foreach (DataRow row in table.Select("id=" + songId))
            {
                row["title"] = titleTextBox.Text;
                row["artist"] = artistTextBox.Text;
                row["album"] = albumTextBox.Text;
                row["genre"] = genreTextBox.Text;
                row["length"] = lengthTextBox.Text;
                row["filename"] = filenameTextBox.Text;
            }
        }

        private void UpdateCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = songIds != null && songIds.Count > 0;
        }

        private void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {            
            if (MessageBox.Show("Are you sure you want to delete this song?", "MiniPlayer",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                string songId = songIdComboBox.SelectedItem.ToString();
                Console.WriteLine("Deleting song " + songId);

                // Search the primary key for the selected song and delete it from 
                // the song table
                DataTable table = musicDataSet.Tables["song"];
                table.Rows.Remove(table.Rows.Find(songId));

                // Remove from playlist_song every occurance of songId.
                // Add rows to a separate list before deleting because we'll get an exception
                // if we try to delete more than one row while looping through table.Rows

                List<DataRow> rows = new List<DataRow>();
                table = musicDataSet.Tables["playlist_song"];
                foreach (DataRow row in table.Rows)
                    if (row["song_id"].ToString() == songId.ToString())
                        rows.Add(row);

                foreach (DataRow row in rows)
                    row.Delete();

                // Remove the song from the combo box and select the next item
                songIds.Remove(songIdComboBox.SelectedItem.ToString());
                if (songIdComboBox.Items.Count > 0)
                    songIdComboBox.SelectedItem = songIdComboBox.Items[0];
                else
                {
                    // No more songs to display
                    titleTextBox.Text = "";
                    artistTextBox.Text = "";
                    albumTextBox.Text = "";
                    genreTextBox.Text = "";
                    lengthTextBox.Text = "";
                    filenameTextBox.Text = "";
                }
            }
        }
        
        private void DeleteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = songIds != null && songIds.Count > 0;
        }

        private void PlayCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mediaPlayer.Open(new Uri(filenameTextBox.Text));
            mediaPlayer.Play();
        }

        private void StopCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mediaPlayer.Stop();            
        }

        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // Save music.xml in the same directory as the exe
            string filename = "music.xml";
            Console.WriteLine("Saving " + filename);
            musicDataSet.WriteXml(filename);
        }
    }
}
