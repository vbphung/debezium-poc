import requests
import json


def create_conn(tables, publication):
    payload = {
        "name": f"poc-{publication}-connector",
        "config": {
            "connector.class": "io.debezium.connector.postgresql.PostgresConnector",
            "database.hostname": "postgres",
            "database.port": "5432",
            "database.user": "replicationuser",
            "database.password": "replicationpassword",
            "database.dbname": "postgres",
            "topic.prefix": "dbzpoc",
            "table.include.list": ",".join(tables),
            "publication.name": publication,
            "slot.name": f"{publication}_slot",
            "plugin.name": "pgoutput",
            "publication.autocreate.mode": "disabled",
            "key.converter": "org.apache.kafka.connect.json.JsonConverter",
            "value.converter": "org.apache.kafka.connect.json.JsonConverter",
        },
    }

    print(f"CREATE CONNECTOR: {payload}")

    try:
        resp = requests.post(
            "http://localhost:8083/connectors",
            headers={"Content-Type": "application/json"},
            data=json.dumps(payload),
        )
        print(f"==> SUCCESSFUL: {resp.text}\n")
    except requests.RequestException as exception:
        print(f"==> FAILED:   {exception}\n")


if __name__ == "__main__":
    create_conn(["business.users"], "dbz_users_pub")
    create_conn(
        ["business.accounts", "business.transactions"],
        "dbz_accounts_transactions_pub",
    )
