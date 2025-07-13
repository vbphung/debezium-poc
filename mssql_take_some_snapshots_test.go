package main

import (
	"crypto/rand"
	"database/sql"
	"encoding/hex"
	"fmt"
	mrand "math/rand"
	"strings"
	"sync"
	"testing"
	"time"

	_ "github.com/microsoft/go-mssqldb"
	"github.com/stretchr/testify/require"
)

const (
	server   = "localhost"
	port     = 1433
	user     = "sa"
	password = "yourStrong(!)Password"
	database = "chill"

	sampleTotal = 10_000
	batchSize   = 300
)

var dbSnapshot = fmt.Sprintf("%s_snap", database)

var firstNames = []string{
	"Logan", "Kendall", "Siobhan", "Roman", "Connor",
	"Tom", "Greg", "Gerri", "Frank", "Karl", "Hugo",
	"Marcia", "Willa", "Ewan", "Rava", "Naomi", "Stewy",
}

var lastNames = []string{
	"Roy", "Wambsgans", "Hirsch", "Royle", "Pierce",
	"Black", "Smith", "Lunden", "McFadyen", "Zissis", "Furness",
	"Macfadyen", "Snook", "Strong", "Culkin", "Braun", "Cox",
}

func TestMain(t *testing.T) {
	masterConnStr := fmt.Sprintf("sqlserver://%s:%s@%s:%d?database=master", user, password, server, port)
	masterDB, err := sql.Open("mssql", masterConnStr)
	require.NoError(t, err)
	defer masterDB.Close()

	dropTable(t, masterDB, dbSnapshot)
	dropTable(t, masterDB, database)

	_, err = masterDB.Exec(fmt.Sprintf("CREATE DATABASE %s", database))
	require.NoError(t, err)

	connStr := fmt.Sprintf("sqlserver://%s:%s@%s:%d?database=%s", user, password, server, port, database)
	db, err := sql.Open("mssql", connStr)
	require.NoError(t, err)
	defer db.Close()
	require.NoError(t, db.Ping())

	createTable(t, db)
	insertRandomAccounts(t, db, sampleTotal)
	takeSnapshot(t, masterDB)

	snapConnStr := fmt.Sprintf("sqlserver://%s:%s@%s:%d?database=%s", user, password, server, port, dbSnapshot)
	snapDB, err := sql.Open("mssql", snapConnStr)
	require.NoError(t, err)

	doesSnapshotwork(t, db, snapDB)
}

func dropTable(t *testing.T, db *sql.DB, dbName string) {
	_, err := db.Exec(fmt.Sprintf("IF DB_ID('%s') IS NOT NULL DROP DATABASE %s", dbName, dbName))
	require.NoError(t, err)
}

func createTable(t *testing.T, db *sql.DB) {
	_, err := db.Exec(`IF OBJECT_ID('accounts', 'U') IS NOT NULL DROP TABLE accounts`)
	require.NoError(t, err)

	_, err = db.Exec(`
		CREATE TABLE accounts (
			ID INT IDENTITY(1,1) PRIMARY KEY,
			username NVARCHAR(50),
			password NVARCHAR(100),
			email NVARCHAR(100),
			fullname NVARCHAR(100),
			created_at DATETIME,
			updated_at DATETIME
		)
	`)
	require.NoError(t, err)
}

func insertRandomAccounts(t *testing.T, db *sql.DB, total int) {
	for i := 0; i < total; i += batchSize {
		tx, err := db.Begin()
		require.NoError(t, err)

		var sb strings.Builder
		sb.WriteString(`INSERT INTO accounts (username, password, email, fullname, created_at, updated_at) VALUES `)

		args := []any{}
		for j := 0; j < batchSize && i+j < total; j++ {
			if j > 0 {
				sb.WriteString(",")
			}
			sb.WriteString("(?, ?, ?, ?, ?, ?)")
			now := time.Now()
			args = append(args,
				fmt.Sprintf("moron%d", i+j),
				randomString(16),
				fmt.Sprintf("moron%d@waystar.com", i+j),
				randomFullName(),
				now,
				now,
			)
		}

		start := time.Now()

		res, err := tx.Exec(sb.String(), args...)
		require.NoError(t, err)

		require.NoError(t, tx.Commit())

		rows, err := res.RowsAffected()
		require.NoError(t, err)

		t.Logf("insert %d fukcing morons after %s", int64(i)+rows, time.Since(start))
	}
}

func takeSnapshot(t *testing.T, masterDB *sql.DB) {
	var logicalName string
	err := masterDB.QueryRow(`
		SELECT name 
		FROM sys.master_files 
		WHERE database_id = DB_ID('chill') AND type_desc = 'ROWS';
	`).Scan(&logicalName)
	require.NoError(t, err)

	snapshotFile := fmt.Sprintf("/var/opt/mssql/snapshots/%s.ss", dbSnapshot)

	snapshotQuery := fmt.Sprintf(`
		CREATE DATABASE %s 
		ON (NAME = %s, FILENAME = '%s') 
		AS SNAPSHOT OF chill;
	`, dbSnapshot, logicalName, snapshotFile)

	start := time.Now()
	_, err = masterDB.Exec(snapshotQuery)
	require.NoError(t, err)

	t.Logf("snapshot '%s' was created in %s", dbSnapshot, time.Since(start))
}

func doesSnapshotwork(t *testing.T, ogDB *sql.DB, snapDB *sql.DB) {
	var wg sync.WaitGroup

	countKendall := func() int {
		var kenCnt int
		err := snapDB.
			QueryRow(`SELECT COUNT(*) FROM accounts WHERE fullname = 'Kendall Roy';`).
			Scan(&kenCnt)
		require.NoError(t, err)

		return kenCnt
	}

	ogKenCnt := countKendall()
	t.Logf("we have %d Kendall's", ogKenCnt)

	wg.Add(2)

	go func() {
		defer wg.Done()
		for range 100 {
			require.Equal(t, ogKenCnt, countKendall())
		}
	}()

	go func() {
		defer wg.Done()
		for range 100 {
			_, err := ogDB.Exec(`
				UPDATE TOP (1) accounts
				SET fullname = 'Our Number One Boy'
				WHERE fullname = 'Kendall Roy';
			`)
			require.NoError(t, err)
		}

		var kenCnt int
		err := ogDB.
			QueryRow(`SELECT COUNT(*) FROM accounts WHERE fullname = 'Kendall Roy';`).
			Scan(&kenCnt)
		require.NoError(t, err)

		t.Logf("now we only have %d Kendall's", kenCnt)
	}()

	wg.Wait()
}

func randomString(n int) string {
	b := make([]byte, n)
	_, _ = rand.Read(b)
	return hex.EncodeToString(b)[:n]
}

func randomFullName() string {
	first := firstNames[mrand.Intn(len(firstNames))]
	last := lastNames[mrand.Intn(len(lastNames))]
	return fmt.Sprintf("%s %s", first, last)
}
