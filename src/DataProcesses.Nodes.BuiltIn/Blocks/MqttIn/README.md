# MQTT_In Block

Subscribes to an MQTT topic filter and emits each valid UTF-8 JSON payload as a JSON Message.

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `message` | Output | JSON Message | Yes | `JsonEnvelope` (`JsonMessage`) |

The emitted envelope uses the broker topic as `topic`, the parsed JSON value as `payload`, and the local receive time as `timestamp`. `correlationId` is unset because MQTT correlation data is not currently mapped.

## Settings

| Field | Type | Default | Notes |
|---|---|---|---|
| `brokerHost` | string | `localhost` | MQTT broker hostname or IP address. |
| `port` | integer | `1883` | TCP port, from 1 through 65535. |
| `clientId` | string | empty | Empty selects a unique ID derived from the Flow node ID. |
| `username` | string | empty | Optional broker username. |
| `password` | string | empty | Optional broker password. Project settings currently store this value as plain text. |
| `useTls` | boolean | `false` | Enables standard certificate-validated TLS. |
| `topic` | string | `dataprocesses/messages` | MQTT topic filter; `+` and `#` wildcards are accepted. |
| `qualityOfService` | integer | `0` | Subscription QoS: 0, 1, or 2. |

MQTT_In connects and subscribes when the flow starts, remains active until the flow is stopped, and retries the connection after an established connection is lost. A payload that is not valid JSON is rejected.