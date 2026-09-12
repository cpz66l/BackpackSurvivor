using System;
using System.IO;
using System.Text;

namespace BS.GamePlay.Save
{
    public static class CampaignFile
    {
        // Publish the fully written file before the caller replaces in-memory state.
        public static bool TryWrite(string path, string json, out string error)
        {
            string temporary = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                error = null;
                return true;
            }
            catch (Exception e) { error = e.Message; return false; }
        }
    }
}
