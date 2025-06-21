import requests
import json


def create_conn(table):
    payload = {
        "name": f"poc-{table}-connector",
        "config": {
            "connector.class": "io.debezium.connector.postgresql.PostgresConnector",
            "database.hostname": "postgres",
            "database.port": "5432",
            "database.user": "replicationuser",
            "database.password": "replicationpassword",
            "database.dbname": "postgres",
            "table.include.list": table,
            "slot.name": f"poc_{str.split(table, '.')[-1]}",
            "topic.prefix": "poc",
            "publication.autocreate.mode": "filtered",
            "plugin.name": "pgoutput",
        },
    }

    try:
        resp = requests.post(
            "http://localhost:8083/connectors",
            headers={"Content-Type": "application/json"},
            data=json.dumps(payload),
        )
        print(f"==> CREATED {payload.get('name')}: {resp.text}")
    except requests.RequestException as exception:
        print(f"==> FAILED {payload.get('name')}:  {exception}")


if __name__ == "__main__":
    create_conn("business.accounts")
    create_conn("business.transactions")
