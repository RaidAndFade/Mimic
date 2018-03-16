using System;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Data.Common;
using System.Diagnostics;

namespace Mimic.Common
{
    public class MySQLDatabase : IDatabase
    {
        private MySqlConnection conn;
        public MySQLDatabase(string host="127.0.0.1", string user="root", string pass="", string database="")
        {
            string connStr = $"Server={host}; database={database}; UID={user}; password={pass}";
            Debug.WriteLine(connStr);
            conn = new MySqlConnection(connStr);
            conn.Open();
        }

        public void createIfNonexistant()
        {
            //don't hardcode these
            MySqlCommand createAccountsTblCmd = new MySqlCommand("CREATE TABLE IF NOT EXISTS `accounts` (`id` INT(11) NOT NULL AUTO_INCREMENT,`username` VARCHAR(255) NOT NULL DEFAULT '',`pass_hash` VARCHAR(255) NOT NULL DEFAULT '',`sessionkey` varchar(255) NOT NULL DEFAULT '',`v` varchar(255) NOT NULL DEFAULT '',`s` varchar(255) NOT NULL DEFAULT '',`token_key` varchar(255) NOT NULL DEFAULT '',`email` varchar(255) NOT NULL DEFAULT '',`join_date` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,`last_ip` varchar(255) NOT NULL DEFAULT '',`last_login` varchar(255) NOT NULL DEFAULT '',`failed_logins` INT(11) NOT NULL DEFAULT '0',`locked` BIT(1) NOT NULL DEFAULT b'0',`lock_country` varchar(255) NOT NULL DEFAULT '',`online` BIT(1) NOT NULL DEFAULT b'0',`locale` SMALLINT(6) NOT NULL DEFAULT '0',`os` varchar(255) NOT NULL DEFAULT '',PRIMARY KEY (`id`),UNIQUE INDEX `username` (`username`))COLLATE='utf8_general_ci'ENGINE=MyISAM;", conn);
            createAccountsTblCmd.ExecuteNonQuery();

            MySqlCommand createTutorialTblCmd = new MySqlCommand("CREATE TABLE IF NOT EXISTS `account_tutorial` (`id` INT(11) NOT NULL,`tut0` INT(11) NOT NULL DEFAULT '0',`tut1` INT(11) NOT NULL DEFAULT '0',`tut2` INT(11) NOT NULL DEFAULT '0',`tut3` INT(11) NOT NULL DEFAULT '0',`tut4` INT(11) NOT NULL DEFAULT '0',`tut5` INT(11) NOT NULL DEFAULT '0',`tut6` INT(11) NOT NULL DEFAULT '0',`tut7` INT(11) NOT NULL DEFAULT '0',PRIMARY KEY (`id`))ENGINE=MyISAM;",conn);
            createTutorialTblCmd.ExecuteNonQuery();
        }

        public async Task init()
        {
        }
        public async Task<AccountInfo> AsyncFetchAccountById(int id)
        {
            AccountInfo info = new AccountInfo();

            MySqlCommand cmd = new MySqlCommand("SELECT * FROM `accounts` WHERE `id`=@id LIMIT 1;",conn);
            cmd.Parameters.AddWithValue("id", id);

            using(var reader = await cmd.ExecuteReaderAsync()){
                if (await reader.ReadAsync())
                {
                    info.id = reader.GetInt32(0);
                    info.username = reader.GetString(1);
                    info.pass_hash = reader.GetString(2);
                    info.sessionkey = reader.GetString(3);
                    info.v = reader.GetString(4);
                    info.s = reader.GetString(5);
                    info.token_key = reader.GetString(6);
                    info.email = reader.GetString(7);
                    info.join_date = reader.GetString(8);
                    info.last_ip = reader.GetString(9);
                    info.last_login = reader.GetString(10);
                    info.failed_logins = reader.GetInt32(11);
                    info.locked = reader.GetBoolean(12);
                    info.lock_country = reader.GetString(13);
                    info.online = reader.GetBoolean(14);
                    info.locale = reader.GetInt16(15);
                    info.os = reader.GetString(16);
                    return info;
                }
                else
                {
                    return null;
                }
            }
        }

        public async Task<AccountInfo> AsyncFetchAccountByName(string name)
        {
            AccountInfo info = new AccountInfo();

            MySqlCommand cmd = new MySqlCommand("SELECT * FROM `accounts` WHERE `username`=@name LIMIT 1;",conn);
            cmd.Parameters.AddWithValue("name", name);

            using(var reader = await cmd.ExecuteReaderAsync()){
                if (await reader.ReadAsync())
                {
                    info.id = reader.GetInt32(0);
                    info.username = reader.GetString(1);
                    info.pass_hash = reader.GetString(2);
                    info.sessionkey = reader.GetString(3);
                    info.v = reader.GetString(4);
                    info.s = reader.GetString(5);
                    info.token_key = reader.GetString(6);
                    info.email = reader.GetString(7);
                    info.join_date = reader.GetString(8);
                    info.last_ip = reader.GetString(9);
                    info.last_login = reader.GetString(10);
                    info.failed_logins = reader.GetInt32(11);
                    info.locked = reader.GetBoolean(12);
                    info.lock_country = reader.GetString(13);
                    info.online = reader.GetBoolean(14);
                    info.locale = reader.GetInt16(15);
                    info.os = reader.GetString(16);
                    return info;
                }
                else
                {
                    return null;
                }
            }
        }

        public async Task<uint[]> AsyncFetchTutorialFlags(int id){
            MySqlCommand cmd = new MySqlCommand("SELECT `tut0`,`tut1`,`tut2`,`tut3`,`tut4`,`tut5`,`tut6`,`tut7` FROM `account_tutorial` WHERE `id`=@id LIMIT 1;",conn);
            cmd.Parameters.AddWithValue("id",id);
            using(var reader = await cmd.ExecuteReaderAsync()){
                if(await reader.ReadAsync()){
                    uint[] flags = new uint[8];
                    for(int i=0;i<8;i++){
                        flags[i] = (uint)reader.GetInt32(i);
                    }
                    return flags;
                }else{
                    return null;
                }
            }
        }

        public async Task AsyncSetTutorialFlags(int id, uint[] flags){
            MySqlCommand cmd = new MySqlCommand("DELETE FROM `account_tutorial` WHERE `id`=@id; INSERT INTO `account_tutorial` VALUES (@id,@t0,@t1,@t2,@t3,@t4,@t5,@t6,@t7);",conn);
            cmd.Parameters.AddWithValue("id",id);
            MySqlParameter[] flagParams = new MySqlParameter[8];
            for(int i=0;i<8;i++){
                flagParams[i] = new MySqlParameter("t"+i,flags[i]);
            }
            cmd.Parameters.AddRange(flagParams);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AsyncUpdateAccount(AccountInfo info)
        {
            MySqlCommand cmd = new MySqlCommand("UPDATE `accounts` SET `v`=@v, `s`=@s,`sessionkey`=@sessionkey, `last_login`=@lastlogin, `email`=@email,`last_ip`=@lastip, `failed_logins`=@failedlogins, `locked`=@locked, `lock_country`=@lock_country, `online`=@online, `locale`=@locale, `os`=@os WHERE `id`=@id",conn);
            cmd.Parameters.AddWithValue("id", info.id);
            cmd.Parameters.AddWithValue("v", info.v);
            cmd.Parameters.AddWithValue("s", info.s);
            cmd.Parameters.AddWithValue("sessionkey", info.sessionkey);
            cmd.Parameters.AddWithValue("email", info.email);
            cmd.Parameters.AddWithValue("lastip", info.last_ip);
            cmd.Parameters.AddWithValue("lastlogin", info.last_login);
            cmd.Parameters.AddWithValue("failedlogins", info.failed_logins);
            cmd.Parameters.AddWithValue("locked", info.locked);
            cmd.Parameters.AddWithValue("lock_country", info.lock_country);
            cmd.Parameters.AddWithValue("online", info.online);
            cmd.Parameters.AddWithValue("locale", info.locale);
            cmd.Parameters.AddWithValue("os", info.os);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
