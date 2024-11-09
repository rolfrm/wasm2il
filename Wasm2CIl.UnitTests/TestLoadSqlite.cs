using System.Reflection;
using Wasm2Cil;
using Wasm2Cil.UnitTests;

namespace Wasm2CIl.UnitTests;

[TestFixture]
public class TestLoadSqlite
{
    [Test]
    public void LoadAndRunSqlite()
    {
        var transformer = new Transformer();
        transformer.LoadImportModule("wasi_snapshot_preview1", typeof(Wasi));
        
        transformer.LoadImportModule("env", typeof(LibC));
        var asm = transformer.LoadWasmAssembly("sqlite3.wasm", "Sqlite");
        var db = asm.Malloc(512);
        var rc00 = asm.Invoke("sqlite3_initialize");
        //var rc0 = asm.Invoke("sqlite3_open", ":memory:", db);
        var rc0 = asm.Invoke("sqlite3_open", "./test.sqlite", db);
        var db1 = asm.GetHeapObject<int>(db);
        var sqlBytes = asm.GetHeapSpan(db, 1024);

        var sql = "CREATE TABLE Users (ID INT PRIMARY KEY NOT NULL, Name TEXT NOT NULL);";
        var buf = asm.Malloc(4);
        var rc = asm.Invoke("sqlite3_exec", db1, sql, 0, 0, buf);
        var span = asm.GetHeapObject<int>(buf);
        var span2 = asm.GetHeapObject<int>(span);
        var span3 = asm.GetHeapObject<int>(span2);
        var err = asm.GetHeapString(span3);
        sql  = "INSERT INTO Users (ID, Name) VALUES (1, 'Alice');";
        var rc2 = asm.Invoke("sqlite3_exec", db1, sql, 0, 0, buf);
         
        sql  = "INSERT INTO Users (ID, Name) VALUES (2, 'Bob');";
        var rc3 = asm.Invoke("sqlite3_exec", db1, sql, 0, 0, buf);
        
        sql = "SELECT ID, Name FROM Users;";
        var stmt = asm.Malloc(4);
        rc = asm.Invoke("sqlite3_prepare_v2", db1, sql, -1, stmt, 0);
        var stmt2 = asm.GetHeapObject<int>(stmt);
        var rc4 = asm.Invoke("sqlite3_step", stmt2);
        var rc5 = asm.Invoke("sqlite3_step", stmt2);
        var rc6 = asm.Invoke("sqlite3_step", stmt2);
        var rc7 = asm.Invoke("sqlite3_finalize", stmt2);
        var memused = asm.Invoke("sqlite3_memory_used");
    }
    
}