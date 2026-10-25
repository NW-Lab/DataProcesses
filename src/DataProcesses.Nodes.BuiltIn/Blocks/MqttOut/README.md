# MQTT_Out Block

Publishes the JSON payload from each incoming JSON Message to an MQTT broker.

## Ports

| ID | Direction | Family | Required | Schema |
|---|---|---|---:|---|
| `message` | Input | JSON Message | Yes | `JsonEnvelope` (`JsonMessage`) |

The MQTT application payload contains only the input message's JSON `payload`, encoded as UTF-8. An Inspector topic overrides the input `topic`; an empty Inspector topic uses the input topic. The envelope timestamp and correlation ID are not serialized into the application payload.

## Settings

| Field | Type | Default | Notes |
|---|---|---|---|
| `brokerHost` | string | `localhost` | MQTT broker hostname or IP address. |
| `port` | integer | `1883` | TCP port, from 1 through 65535. |
| `clientId` | string | empty | Empty selects a unique ID derived from the Flow node ID. |
| `username` | string | empty | Optional broker username. |
| `password` | string | empty | Optional broker password. Project settings currently store this value as plain text. |
| `useTls` | boolean | `false` | Enables standard certificate-validated TLS. |
| `topic` | string | empty | Optional publish topic override. MQTT publish topics cannot contain wildcards. |
| `qualityOfService` | integer | `0` | Publish QoS: 0, 1, or 2. |
| `retain` | boolean | `false` | Requests that the broker retain the latest message for the topic. |

When the topic setting is empty, each incoming message must provide a non-empty topic. The node connects on the first received packet and disposes its client when the flow stops.