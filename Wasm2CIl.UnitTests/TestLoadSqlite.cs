using System.Diagnostics;
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
        transformer.LoadOverrideModule(typeof(LibC.LibCOverride));
        var asm = transformer.LoadWasmAssembly("sqlite3.wasm", "SqliteWasm", "SqliteWasm.dll");
        
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
        SqliteWasm.C.sqlite3_initialize();
        var w = new WasmAssembly(typeof(SqliteWasm.C).Assembly);
        File.Delete("./test.3.sqlite");
        var str = w.StringToHeap("./test.3.sqlite");
        var db = w.Malloc(4);
        int ok = SqliteWasm.C.sqlite3_open(str, db);
        var db2 = w.GetHeapObject<int>(db);
        var sql2 = "CREATE TABLE IF NOT EXISTS Users (ID INT PRIMARY KEY NOT NULL, Name TEXT NOT NULL);";
        var sql2p = w.StringToHeap(sql2);
        int ok3 = SqliteWasm.C.sqlite3_exec(db2, sql2p, 0, 0, 0);

        var insertStmt = "INSERT INTO Users (ID, Name) VALUES (?, ?);";
        var stmt0 = w.Malloc(4);
        SqliteWasm.C.sqlite3_prepare_v2(db2, w.StringToHeap(insertStmt), -1, stmt0, 0);
        var stmt0_ = w.GetHeapObject<int>(stmt0);
        var t = w.StringToHeap("TEstTest");

        var sw = Stopwatch.StartNew();
        int ok5 = SqliteWasm.C.sqlite3_exec(db2, w.StringToHeap("BEGIN TRANSACTION;"), 0, 0, 0);
        for (int i = 0; i < 100; i++)
        {
            SqliteWasm.C.sqlite3_bind_int(stmt0_, 1, i);
            SqliteWasm.C.sqlite3_bind_text(stmt0_, 2,  t, -1, 0);
            SqliteWasm.C.sqlite3_step(stmt0_);
            SqliteWasm.C.sqlite3_reset(stmt0_);
            //SqliteWasm.C.sqlite3_clear_bindings(stmt0_);
        }
        int ok6 = SqliteWasm.C.sqlite3_exec(db2, w.StringToHeap("COMMIT;"), 0, 0, 0);
        if (ok6 == 11)
            throw new Exception("Database corrupt!");
        Console.WriteLine($"time: {sw.Elapsed.TotalSeconds}");
        // pre optimize takes about 9.96s
        // after removing bounds checks I got it down to 7.38s
        var sql  = "SELECT ID, Name FROM Users;";
        var str2 = w.StringToHeap(sql);
        var stmt = w.Malloc(4);
        
        
        int ok2 = SqliteWasm.C.sqlite3_prepare_v2(db2, str2, -1, stmt, 0);
        var stmt_ = w.GetHeapObject<int>(stmt);
        if (ok2 != 0)
        {
            
            var err = SqliteWasm.C.sqlite3_errmsg(db2);
            var errstr = w.GetHeapString(err);
        }

        int j = 0;
        while (true)
        {
            int rc3 = SqliteWasm.C.sqlite3_step(stmt_);
            if (rc3 != 100)
                break;
            j++;
        }
        Console.WriteLine($"Step: {j}");

        SqliteWasm.C.sqlite3_close(db);
        
    }

}