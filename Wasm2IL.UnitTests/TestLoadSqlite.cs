using System.Diagnostics;
using Wasm2IL;
using Wasm2IL.UnitTests;
using Assert = Wasm2IL.UnitTests.Assert;

namespace Wasm2CIl.UnitTests;

[TestFixture]
public class TestLoadSqlite
{

    
    public enum SqliteErrorCode : int
    {
        
    }
    
    public interface IBadSqliteApi
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
    
    public interface ISqliteApi
    {
        [Wasm("sqlite3_initialize")]
        public SqliteErrorCode sqlite3_initialize();
        
        [Wasm("sqlite3_open_v2")]
        public SqliteErrorCode sqlite3_open_v2(string connection, int db, int flags, int vfs);

        [Wasm("sqlite3_exec")]
        public SqliteErrorCode sqlite3_exec(int db, string command,  int callback, int arg, int errorMsg);
        
        //"sqlite3_prepare_v2", db1, sql, -1, stmt, 0);
        [Wasm("sqlite3_prepare_v2")]
        public SqliteErrorCode sqlite3_prepare_v2(int db, string sql, int nByte, int stmt, int tail_0);

        [Wasm("sqlite3_open")]
        int sqlite3_open(string str, int db);

        int sqlite3_bind_int(int stmt0, int i, int i1);
        int sqlite3_bind_text(int stmt0, int i, int i1, int i2, int i3);
        int sqlite3_step(int stmt0);
        int sqlite3_reset(int stmt0);
        int sqlite3_errmsg(int db2);
        int sqlite3_close(int db);
        int sqlite3_extended_errcode(int db2);
    }

    private static WasmAssembly built = null;
    static WasmAssembly buildSqlite()
    {
        if (built != null)
            return built;
        
        var transformer = new Transformer();
        
        transformer.LoadImportModule("env", typeof(LibC));
        transformer.LoadOverrideModule(typeof(LibC.LibCOverride));
        built = transformer.LoadWasmAssembly("sqlite3.wasm", "SqliteWasm", "SqliteWasm.dll");
        return built;
    }
    
    [Test]
    public void LoadAndRunSqlite()
    {
        var asm = buildSqlite();
        
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
        var w = built;
        var SqliteWasm =w.AsImplementation<ISqliteApi>();
        SqliteWasm.sqlite3_initialize();
        
        File.Delete("./test.3.sqlite");
        var db = w.Malloc(4);
        int ok = SqliteWasm.sqlite3_open("./test.3.sqlite", db);
        var db2 = w.GetHeapObject<int>(db);
        var sql2 = "CREATE TABLE IF NOT EXISTS Users (ID INT PRIMARY KEY NOT NULL, Name TEXT NOT NULL);";
        var sql2p = w.StringToHeap(sql2);
        SqliteErrorCode ok3 = SqliteWasm.sqlite3_exec(db2, sql2, 0, 0, 0);

        var insertStmt = "INSERT INTO Users (ID, Name) VALUES (?, ?);";
        var stmt0 = w.Malloc(4);
        SqliteWasm.sqlite3_prepare_v2(db2, insertStmt, -1, stmt0, 0);
        var stmt0_ = w.GetHeapObject<int>(stmt0);
        var t = w.StringToHeap("TEstTest");

        var sw = Stopwatch.StartNew();
        var ok5 = SqliteWasm.sqlite3_exec(db2, "BEGIN TRANSACTION;", 0, 0, 0);
        for (int i = 0; i < 100; i++)
        {
            SqliteWasm.sqlite3_bind_int(stmt0_, 1, i);
            SqliteWasm.sqlite3_bind_text(stmt0_, 2,  t, -1, 0);
            SqliteWasm.sqlite3_step(stmt0_);
            SqliteWasm.sqlite3_reset(stmt0_);
            //SqliteWasm.C.sqlite3_clear_bindings(stmt0_);
        }
        var ok6 = (int)SqliteWasm.sqlite3_exec(db2, "COMMIT;", 0, 0, 0);
        if (ok6 == 11)
            throw new Exception("Database corrupt!");
        Console.WriteLine($"time: {sw.Elapsed.TotalSeconds}");
        // pre optimize takes about 9.96s
        // after removing bounds checks I got it down to 7.38s
        var sql  = "SELECT ID, Name FROM Users;";
        var str2 = w.StringToHeap(sql);
        var stmt = w.Malloc(4);
        
        
        int ok2 = (int)SqliteWasm.sqlite3_prepare_v2(db2, sql, -1, stmt, 0);
        var stmt_ = w.GetHeapObject<int>(stmt);
        if (ok2 != 0)
        {
            
            var err = SqliteWasm.sqlite3_errmsg(db2);
            var errstr = w.GetHeapString(err);
        }

        int j = 0;
        while (true)
        {
            int rc3 = SqliteWasm.sqlite3_step(stmt_);
            if (rc3 != 100)
                break;
            j++;
        }
        Console.WriteLine($"Step: {j}");

        SqliteWasm.sqlite3_close(db);

        try
        {
            w.AsImplementation<IBadSqliteApi>();
            throw new Exception("This should have thrown");
        }
        catch(ImplementException)
        {
            
        }
        
        var api = w.AsImplementation<ISqliteApi>();
        

    }
    
    private string sqlitePerfTest0 = @"
  
  DROP TABLE IF EXISTS customers;
  
  CREATE TABLE customers (
      id INTEGER PRIMARY KEY
  );
  
  WITH RECURSIVE c(i) AS (
      SELECT 1
      UNION ALL SELECT i+1 FROM c WHERE i < 129
  )
  INSERT INTO customers (id)
  SELECT
      i
  FROM c;
  ";
    private string sqlitePerfTest2 = @"
  
  DROP TABLE IF EXISTS customers;
  
  CREATE TABLE customers (
      id INTEGER PRIMARY KEY
  );
  
  WITH RECURSIVE c(i) AS (
      SELECT 1
      UNION ALL SELECT i+1 FROM c WHERE i < 1290000
  )
  INSERT INTO customers (id)
  SELECT
      i
  FROM c;
  ";
    
    private string sqlitePerfTest3 = @"
  PRAGMA temp_store_directory = './data';
  DROP TABLE IF EXISTS customers;
   DROP TABLE IF EXISTS items;
   
  CREATE TABLE customers (
      id INTEGER PRIMARY KEY,
	  v2 REAL,
	  v3 REAL
  );
  
   CREATE TABLE items (
      id INTEGER PRIMARY KEY,
	  owner INTEGER,
	  name TEXT
  );
  
  WITH RECURSIVE c(i) AS (
      SELECT 1
      UNION ALL SELECT i+1 FROM c WHERE i < 60800
  )
  INSERT INTO customers (id, v2, v3)
  SELECT
      i + 5, (i * 3.0), (i * 3.0 * 3.0)
  FROM c;
  
  insert INTO items (id, owner, name)
  SELECT (customers.id * 2), customers.id, ""thing""
  FROM customers;
  
 SELECT SUM(customers.id),  SUM(items.id), COUNT(items.id), COUNT(customers.id) FROM customers  JOIN items ON customers.id = items.owner;
  
  ";

    private string sqliteFreeBlob =
        "CREATE TABLE t(x);\nINSERT INTO t VALUES(zeroblob(500*1024*1024));  -- 500 MB\nDROP TABLE t;";
    
    public TestLoadSqlite()
    {
        buildSqlite();
    }
    [Test]
    public void LoadAndRunSqliteError()
    {
        // This was used to find a bug related to extending 129 to a full int.
        var w = built;
        var SqliteWasm = w.AsImplementation<ISqliteApi>();
        w.Invoke("sqlite3_initialize");
        
        File.Delete("./test_bug.sqlite");
        var db = w.Malloc(4);
        int ok = (int)SqliteWasm.sqlite3_open("./test_bug.sqlite", db);
        var db2 = w.GetHeapObject<int>(db);
        var sql2 = sqlitePerfTest0;
        int ok3 = (int)SqliteWasm.sqlite3_exec(db2, sql2.Replace("\r", ""), 0, 0, 0);
        SqliteWasm.sqlite3_close(db2);
        File.Delete("./test_bug.sqlite");
        
        File.Delete("./test_bug.sqlite");
        SqliteWasm.sqlite3_open("./test_bug.sqlite", db);
        db2 = w.GetHeapObject<int>(db);
        var sql3 = sqlitePerfTest2;
        int ok4 = (int)SqliteWasm.sqlite3_exec(db2, sql3.Replace("\r", ""), 0, 0, 0);
        //var vacuum = w.StringToHeap("VACUUM;");

        //int ok5 = SqliteWasm.C.sqlite3_exec(db2, vacuum, 0, 0, 0);
        SqliteWasm.sqlite3_close(db2);
        File.Delete("./test_bug.sqlite");

        Assert.AreEqual(ok3, 0);
        //Assert.AreEqual(ok5, 0);

    }

        private string sqlitePerfTestxx = @"
-- ============================================
--  Basic SQLite Benchmark Script (No datetime)
-- ============================================

PRAGMA journal_mode = DELETE;
PRAGMA synchronous = NORMAL;
PRAGMA temp_store = MEMORY;

-- Drop tables if they exist
DROP TABLE IF EXISTS customers;
DROP TABLE IF EXISTS orders;
DROP TABLE IF EXISTS line_items;

-- ============================================
-- 1. CREATE TABLES
-- ============================================

CREATE TABLE customers (
    id INTEGER PRIMARY KEY,
    name TEXT,
    email TEXT
);

CREATE TABLE orders (
    id INTEGER PRIMARY KEY,
    customer_id INTEGER,
    status TEXT,
    amount REAL
);

CREATE TABLE line_items (
    id INTEGER PRIMARY KEY,
    order_id INTEGER,
    product_id INTEGER,
    quantity INTEGER,
    price REAL
);

-- ============================================
-- 2. INSERT SYNTHETIC DATA
--    Uses simple SQLite recursive loops
-- ============================================

-- Insert 10,000 customers
WITH RECURSIVE c(i) AS (
    SELECT 1
    UNION ALL SELECT i+1 FROM c WHERE i < 300000
)
INSERT INTO customers (id, name, email)
SELECT
    i,
    'Customer ' || i,
    'customer' || i || '@example.com'
FROM c;

-- Insert 100,000 orders
WITH RECURSIVE o(i) AS (
    SELECT 1
    UNION ALL SELECT i+1 FROM o WHERE i < 1000000
)
INSERT INTO orders (id, customer_id, status, amount)
SELECT
    i,
    i + 1,
    CASE WHEN (i % 10) < 8 THEN 'completed' ELSE 'cancelled' END,
    (i % 500) + 1
FROM o;

-- Insert 300,000 line items
WITH RECURSIVE l(i) AS (
    SELECT 1
    UNION ALL SELECT i+1 FROM l WHERE i < 3000000
)
INSERT INTO line_items (id, order_id, product_id, quantity, price)
SELECT
    i,
    (i % 100000) + 1,
    (i % 20000) + 1,
    (i % 10) + 1,
    (i % 200) + 1
FROM l;

-- ============================================
-- 3. RUN BENCHMARK QUERIES
-- ============================================

-- Heavy join + aggregation
SELECT c.id, c.name, SUM(li.quantity * li.price) AS total_spent
FROM customers c
JOIN orders o ON c.id = o.customer_id
JOIN line_items li ON o.id = li.order_id
GROUP BY c.id
ORDER BY total_spent DESC
LIMIT 20;

-- Grouping test
SELECT status, COUNT(*), AVG(amount)
FROM orders
GROUP BY status;

-- Join + filter + sort
SELECT o.id, c.name, o.amount
FROM orders o
JOIN customers c ON o.customer_id = c.id
WHERE o.amount > 400
ORDER BY o.amount DESC
LIMIT 50;

-- Simple lookups
SELECT * FROM orders WHERE id = 1;
SELECT * FROM orders WHERE id = 50000;
SELECT * FROM orders WHERE id = 99999;

-- Heavy filter on large table
SELECT *
FROM line_items
WHERE price > 150
ORDER BY price DESC
LIMIT 200;
    
-- ============================================
-- End of benchmark
-- ============================================

";
    [Test]
    public void SqliteRationalityTest()
    {
        // This was used to find a bug related to extending 129 to a full int.
        var w = built;
        var SqliteWasm = w.AsImplementation<ISqliteApi>();
        w.Invoke("sqlite3_initialize");

        File.Delete("./test_bug2.sqlite");
        var db = w.Malloc(4);
        int ok = (int)SqliteWasm.sqlite3_open_v2("./test_bug2.sqlite", db, 6, 0);
        var db2 = w.GetHeapObject<int>(db);
        var sql2 = sqlitePerfTest3;
        var sw = Stopwatch.StartNew();
        int ok3 = (int)SqliteWasm.sqlite3_exec(db2, sqlitePerfTestxx.Replace("\r", ""), 0, 0, 0);
        var elapsed = sw.Elapsed.TotalSeconds;
        if (ok3 != 0)
        {
            throw new Exception("failed call");
        }
        var c = SqliteWasm.sqlite3_errmsg(db2);
        var msg  = w.GetHeapString(c);

        int ok5 = (int)SqliteWasm.sqlite3_exec(db2, "VACUUM;", 0, 0, 0);
        //int ok7 = SqliteWasm.C.sqlite3_exec(db2, vacuum, 0, 0, 0);
        var c2 = SqliteWasm.sqlite3_errmsg(db2);
        var ok6 = SqliteWasm.sqlite3_extended_errcode(db2);
        
        var msg2  = w.GetHeapString(c2);
        SqliteWasm.sqlite3_close(db2);
        //File.Delete("./test_bug2.sqlite");

        //if (ok5!= 0)
        //    throw new Exception(msg2);

    }

}