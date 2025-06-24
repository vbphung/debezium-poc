-- Initialize your schema and tables
CREATE SCHEMA IF NOT EXISTS business;

CREATE TABLE IF NOT EXISTS business.users (
    id SERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    email TEXT,
    phone TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS business.accounts (
    id SERIAL PRIMARY KEY,
    owner_id INTEGER NOT NULL,
    balance NUMERIC(12, 2) NOT NULL,
    CONSTRAINT fk_owner FOREIGN KEY (owner_id) REFERENCES business.users(id)
);

CREATE TABLE IF NOT EXISTS business.transactions (
    id SERIAL PRIMARY KEY,
    sender_id INTEGER NOT NULL,
    recipient_id INTEGER NOT NULL,
    amount NUMERIC(12, 2) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_sender FOREIGN KEY (sender_id) REFERENCES business.accounts(id),
    CONSTRAINT fk_recipient FOREIGN KEY (recipient_id) REFERENCES business.accounts(id)
);

-- Enable logical replication
-- `max_replication_slots` and `max_wal_senders` can be any number
ALTER SYSTEM
SET wal_level = logical;

ALTER SYSTEM
SET max_replication_slots = 10;

ALTER SYSTEM
SET max_wal_senders = 10;

-- Create a REPLICATION role and grant it USAGE privilege on your schema
CREATE ROLE replicationuser WITH LOGIN REPLICATION PASSWORD 'replicationpassword';

GRANT USAGE ON SCHEMA business TO replicationuser;

-- Create a PUBLICATION for a table
-- Grant SELECT privilege for the REPLICATION role
CREATE PUBLICATION dbz_users_pub FOR TABLE business.users;

GRANT SELECT ON business.users TO replicationuser;

-- Setting up REPLICA IDENTITY FULL is optional
-- but super useful because it allows Debezium to
-- capture the previous state of rows.
-- By default, the change events only contain the final state.
ALTER TABLE business.users REPLICA IDENTITY FULL;

-- PUBLICATION for a group of correlated tables
CREATE PUBLICATION dbz_accounts_transactions_pub FOR TABLE business.accounts,
business.transactions;

GRANT SELECT ON business.accounts TO replicationuser;

GRANT SELECT ON business.transactions TO replicationuser;

-- (Optional)
ALTER TABLE business.accounts REPLICA IDENTITY FULL;

ALTER TABLE business.transactions REPLICA IDENTITY FULL;

-- Insert some sample data to see events
-- It should work also for UPDATEs and DELETEs but I was too lazy
INSERT INTO business.users (name, email, phone)
VALUES ('Alice', 'alice@example.com', '0123456789'),
    ('Bob', 'bob@example.com', '0987654321'),
    ('Charlie', 'charlie@example.com', '0112233445'),
    ('David', 'david@example.com', '0223344556'),
    ('Eve', 'eve@example.com', '0334455667'),
    ('Frank', 'frank@example.com', '0445566778'),
    ('Grace', 'grace@example.com', '0556677889');

INSERT INTO business.accounts (owner_id, balance)
VALUES (1, 1000.00),
    (2, 850.50),
    (3, 1320.75),
    (4, 440.25),
    (5, 3000.00),
    (6, 2150.00),
    (7, 1225.00);

INSERT INTO business.transactions (sender_id, recipient_id, amount)
VALUES (1, 2, 100.00),
    (2, 3, 150.50),
    (3, 4, 200.75),
    (4, 5, 50.25),
    (5, 6, 300.00),
    (6, 7, 75.00),
    (7, 1, 125.25),
    (1, 3, 90.00),
    (2, 5, 60.60),
    (3, 6, 110.10);
