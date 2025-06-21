ALTER SYSTEM
SET wal_level = logical;

ALTER SYSTEM
SET max_replication_slots = 10;

ALTER SYSTEM
SET max_wal_senders = 10;

CREATE USER replicationuser WITH PASSWORD 'replicationpassword';

ALTER USER replicationuser WITH REPLICATION;

ALTER USER replicationuser REPLICATION;

GRANT CONNECT ON DATABASE postgres TO replicationuser;

GRANT USAGE ON SCHEMA public TO replicationuser;

GRANT SELECT ON ALL TABLES IN SCHEMA public TO replicationuser;
