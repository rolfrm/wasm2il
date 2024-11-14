#include "sqlite3.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

int main() {
    sqlite3 *db;
    char *errMsg = 0;
    int rc;

    // Initialize SQLite
    rc = sqlite3_initialize();
    if (rc != SQLITE_OK) {
        fprintf(stderr, "Cannot initialize SQLite: %s\n", sqlite3_errmsg(db));
        return rc;
    }

    // Delete the file if it exists (similar to File.Delete in C#)
    remove("./test.3.sqlite");

    // Open database
    rc = sqlite3_open("./test.3.sqlite", &db);
    if (rc != SQLITE_OK) {
        fprintf(stderr, "Cannot open database: %s\n", sqlite3_errmsg(db));
        sqlite3_close(db);
        return rc;
    }

    // Create table
    const char *sql_create = "CREATE TABLE IF NOT EXISTS Users (ID INT PRIMARY KEY NOT NULL, Name TEXT NOT NULL);";
    rc = sqlite3_exec(db, sql_create, 0, 0, &errMsg);
    if (rc != SQLITE_OK) {
        fprintf(stderr, "SQL error: %s\n", errMsg);
        sqlite3_free(errMsg);
        sqlite3_close(db);
        return rc;
    }

    // Prepare insert statement
    const char *sql_insert = "INSERT INTO Users (ID, Name) VALUES (?, ?);";
    sqlite3_stmt *stmt;
    rc = sqlite3_prepare_v2(db, sql_insert, -1, &stmt, 0);
    if (rc != SQLITE_OK) {
        fprintf(stderr, "Failed to prepare statement: %s\n", sqlite3_errmsg(db));
        sqlite3_close(db);
        return rc;
    }

    // Begin transaction
    rc = sqlite3_exec(db, "BEGIN TRANSACTION;", 0, 0, &errMsg);
    if (rc != SQLITE_OK) {
        fprintf(stderr, "Failed to begin transaction: %s\n", errMsg);
        sqlite3_free(errMsg);
        sqlite3_close(db);
        return rc;
    }

    // Insert 3 million rows
    const char *name = "TEstTest";
    clock_t start = clock();
    for (int i = 0; i < 10000000; i++) {
        sqlite3_bind_int(stmt, 1, i);
        sqlite3_bind_text(stmt, 2, name, -1, SQLITE_STATIC);
        rc = sqlite3_step(stmt);
        if (rc != SQLITE_DONE) {
            fprintf(stderr, "Failed to execute statement: %s\n", sqlite3_errmsg(db));
            sqlite3_close(db);
            return rc;
        }
        sqlite3_reset(stmt);
    }
    
    // Commit transaction
    rc = sqlite3_exec(db, "COMMIT;", 0, 0, &errMsg);
    if (rc != SQLITE_OK) {
        fprintf(stderr, "Failed to commit transaction: %s\n", errMsg);
        sqlite3_free(errMsg);
        sqlite3_close(db);
        return rc;
    }

    double elapsed = (double)(clock() - start) / CLOCKS_PER_SEC;
    printf("time: %f seconds\n", elapsed);

    // Select from table
    const char *sql_select = "SELECT ID, Name FROM Users;";
    rc = sqlite3_prepare_v2(db, sql_select, -1, &stmt, 0);
    if (rc != SQLITE_OK) {
        fprintf(stderr, "Failed to prepare select statement: %s\n", sqlite3_errmsg(db));
        sqlite3_close(db);
        return rc;
    }

    int rowCount = 0;
    while ((rc = sqlite3_step(stmt)) == SQLITE_ROW) {
        rowCount++;
    }
    printf("Step: %d\n", rowCount);

    // Cleanup
    sqlite3_finalize(stmt);
    sqlite3_close(db);

    return 0;
}