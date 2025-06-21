CREATE SCHEMA IF NOT EXISTS business;

CREATE TABLE IF NOT EXISTS business.accounts (
    id SERIAL PRIMARY KEY,
    name TEXT NOT NULL
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
