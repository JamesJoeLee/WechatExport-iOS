using System.Collections.Generic;
using System.Data.SQLite;
using mbdbdump;
using System;
using System.IO;

namespace WechatExport
{
    public static class V10db
    {

        //BackupDB参数是Manifest.db的地址
        //C:\Users\86233\AppData\Roaming\Apple Computer\MobileSync\Backup\00008020-000D083034BB002E\Manifest.db
        //Domain:    com.tencent.xin  这个可能是跟微信相关的一个文件不知道是什么
        //Domain:英文解释域
        public static List<MBFileRecord> ReadMBDB(string BackupDB, string Domain)
        {
            try
            {
                var files = new List<MBFileRecord>();
                using (var conn = new SQLiteConnection())
                {
                    conn.ConnectionString = "data source=" + BackupDB + ";version=3";
                    conn.Open();
                    using (var cmd = new SQLiteCommand(conn))
                    {
                        cmd.CommandText = "SELECT fileID,relativePath FROM Files WHERE domain='AppDomain-" + Domain + "'";
                        using (var reader = cmd.ExecuteReader())
                            while (reader.Read())
                            {
                                var key = reader.GetString(0);
                                key = Path.Combine(key.Substring(0, 2), key);
                                var path = reader.GetString(1);
                                files.Add(new MBFileRecord()
                                {
                                    Path = path,
                                    key = key
                                });
                            }
                    }
                }
                return files;
            }
            catch (Exception) { return null; }
        }
    }
}
