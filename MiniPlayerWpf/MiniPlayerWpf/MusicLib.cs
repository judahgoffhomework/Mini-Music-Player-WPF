using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniPlayerWpf
{
    public class MusicLib
    {
        private DataSet musicDataSet;

        public MusicLib()
        {
            musicDataSet = new DataSet();
            musicDataSet.ReadXmlSchema("music.xsd");
            musicDataSet.ReadXml("music.xml");
        }

        public void AddSong(Song song)
        {
            // Add the selected file to the song table
            DataTable table = musicDataSet.Tables["song"];
            DataRow row = table.NewRow();

            row["title"] = song.Title;
            row["artist"] = song.Artist;
            row["album"] = song.Album;
            row["filename"] = song.Filename;
            row["length"] = song.Length;
            row["genre"] = song.Genre;
            table.Rows.Add(row);

            // Now we know the ID
            song.Id = Convert.ToInt32(row["id"]);
        }

        public Song GetSong(int songId)
        {
            Song song = null;
            DataTable table = musicDataSet.Tables["song"];

            // Only one row should be selected
            foreach (DataRow row in table.Select("id=" + songId))
            {
                song = new Song();
                song.Id = songId;
                song.Title = row["title"].ToString();
                song.Artist = row["artist"].ToString();
                song.Album = row["album"].ToString();
                song.Genre = row["genre"].ToString();
                song.Length = row["length"].ToString();
                song.Filename = row["filename"].ToString();
            }

            return song;
        }
    }
}
