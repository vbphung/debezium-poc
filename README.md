# Debezium POC

## Steps

1. ```shell
   docker-compose up -d
   ```
2. Create some tables and (optional) insert some data
3. Enable logical WAL

```sql
ALTER SYSTEM SET wal_level = logical;
ALTER SYSTEM SET max_replication_slots = 10;
ALTER SYSTEM SET max_wal_senders = 10;
```

4. Restart Postgres
5. ```sql
   CREATE ROLE replicationuser WITH LOGIN REPLICATION PASSWORD 'replicationpassword';
   ```
6. Create publications for groups of closely related tables (or a single table)
7. Grant `replicationuser` user `SELECT` privilege on all above tables

```sql
CREATE PUBLICATION dbz_accounts_transactions_pub FOR TABLE business.accounts, business.transactions;
GRANT SELECT ON business.accounts TO replicationuser;
GRANT SELECT ON business.transactions TO replicationuser;
```

8. Restart Postgres
9. ```shell
   python3 ./postgres_create_connection.py
   ```

   (You might want to change something in `postgres_create_connection.py` before running)

## Then?

### Everything was so messy

- After running `postgres_create_connection.py`, some topics were created as expected, some weren't
- With some missing topics, after I tried to insert some data into its table, the topic was created, but some weren't
- But finally, after scrolling Instagram for 5m, all missing topics were created. So did I need to insert more data to trigger the topic creation?

> **Answer:** Yes, just wait, the miracle will come

### To list all topics

```shell
docker exec -it debezium-poc-kafka kafka-topics \
  --bootstrap-server localhost:9092 \
  --list
```

### To listen to a topic

```shell
docker exec -it debezium-poc-kafka kafka-console-consumer \
  --bootstrap-server kafka:9092 \
  --topic your_topic_name \
  --from-beginning
```

(Remove `--from-beginning` if you don't want to go from beginning)
