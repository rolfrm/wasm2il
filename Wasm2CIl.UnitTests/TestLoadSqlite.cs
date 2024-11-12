using System.Runtime.InteropServices;
using Wasm2Cil;
using Wasm2Cil.UnitTests;

namespace Wasm2CIl.UnitTests;

[TestFixture]
public class TestLoadSqlite
{

    public class WasmAttribute : Attribute
    {
        public WasmAttribute(string importName)
        {
            
        }

        public WasmAttribute()
        {
            
        }
    }

    public enum SqliteErrorCode
    {
        
    }
    
    public interface ISqliteApi
    {
        [Wasm("sqlite3_initialize")]
        public void Initialize();
        
        [Wasm("sqlite3_open_v2")]
        public void Open_V2(string connection, out int db, int flags, int vfs);

        [Wasm("sqlite3_exec")]
        public SqliteErrorCode Exec(string command, int db, int callback, int arg, int errorMsg);
        
        //"sqlite3_prepare_v2", db1, sql, -1, stmt, 0);
        [Wasm("sqlite3_prepare_v2")]
        public void Prepare(int db, string sql, int nByte, int stmt, int tail_0);
    }
    
    [Test]
    public void LoadAndRunSqlite()
    {
        var transformer = new Transformer();
        
        transformer.LoadImportModule("env", typeof(LibC));
        var asm = transformer.LoadWasmAssembly("sqlite3.wasm", "Sqlite");//, "Sqlite.Wasm.dll");
        
        var db = asm.Malloc(4);
        var rc00 = asm.Invoke("sqlite3_initialize");
        //var rc0 = asm.Invoke("sqlite3_open", ":memory:", db);
        var rc0 = asm.Invoke("sqlite3_open_v2", "./test.2.sqlite", db, 2, 0);
        var db1 = asm.GetHeapObject<int>(db);

        string sql;
        object rc;
        
        sql = "CREATE TABLE IF NOT EXISTS Users (ID INT PRIMARY KEY NOT NULL, Name TEXT NOT NULL);";
        var buf = asm.Malloc(4);
        rc = asm.Invoke("sqlite3_exec", db1, sql, 0, 0, buf);

        void checkError(int r)
        {
            if (r != 0)
            {
                var span = asm.GetHeapObject<int>(buf);
                var str = new CString(asm.GetHeap(), span);
                    throw new Exception(str.ToString());
            }
        }

        var r = new Random();

        checkError((int)rc);
        sql  = $"INSERT INTO Users (ID, Name) VALUES ({r.Next()}, 'Alice');";
        var rc2 = asm.Invoke("sqlite3_exec", db1, sql, 0, 0, buf);
        checkError((int) rc2);
        if (true)
        {
            sql = $"INSERT INTO Users (ID, Name) VALUES ({r.Next()}, 'Bob');";
            var rc3 = (int) asm.Invoke("sqlite3_exec", db1, sql, 0, 0, buf);
            if (rc3 != 0)
            {
                var span = asm.GetHeapObject<int>(buf);
                var str = new CString(asm.GetHeap(), span);
                throw new Exception(str.ToString());
            }
        }

        sql = "SELECT ID, Name FROM Users;";
        var stmt = asm.Malloc(4);
        rc = asm.Invoke("sqlite3_prepare_v2", db1, sql, -1, stmt, 0);
        var stmt2 = asm.GetHeapObject<int>(stmt);
        while (true)
        {
            var rc4 = (int)asm.Invoke("sqlite3_step", stmt2);
            int id = (int)asm.Invoke("sqlite3_column_int", stmt2, 0);
            int offset = (int)asm.Invoke("sqlite3_column_text", stmt2, 1);
            var str = asm.GetHeapString(offset);
            Console.WriteLine($"{id}: {str}");
            
            if (rc4 != 100)
                break;

        }
        var memused = asm.Invoke("sqlite3_memory_used");
        asm.Invoke("sqlite3_close", db);
    }
    
    [Test]
    public void LoadAndRunSqlite2()
    {
        /*Sqlite.Wasm.Code.sqlite3_initialize();
        var w = new WasmAssembly(typeof(Sqlite.Wasm.Code).Assembly);
        var str = w.StringToHeap("./test.sqlite");
        var db = w.Malloc(4);
        int ok = Sqlite.Wasm.Code.sqlite3_open(str, db);
        var sql  = "SELECT ID, Name FROM Users;";
        var str2 = w.StringToHeap(sql);
        var stmt = w.Malloc(4);
        
        var db2 = w.GetHeapObject<int>(db);
        int ok2 = Sqlite.Wasm.Code.sqlite3_prepare_v2(db2, str2, -1, stmt, 0);
        if (ok2 != 0)
        {
            
            var err = Sqlite.Wasm.Code.sqlite3_errmsg(db2);
            var errstr = w.GetHeapString(err);
        }*/
    }

}