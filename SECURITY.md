# Security Policy

## Project status

DataProcesses is currently pre-alpha software. No release should yet be treated as suitable for safety-critical, clinical, diagnostic, or production data-acquisition use.

## Supported versions

Until the first tagged release, security fixes are applied only to the default branch. After releases begin, this table will identify supported version lines.

| Version | Supported |
|---|---|
| Default branch | Yes |
| Unreleased local forks | No |

## Reporting a vulnerability

Please do not disclose a suspected vulnerability in a public issue, discussion, or pull request. Prefer GitHub’s private vulnerability reporting feature when it is available for this repository. Otherwise, contact the repository owner using a private address published on the owner’s GitHub profile and include `DataProcesses security` in the subject.

A useful report contains the affected revision, environment, reproduction steps, expected and observed behavior, likely impact, and any suggested mitigation. Do not include real personal, health, biometric, or credential data in reproduction material.

Maintainers will acknowledge a complete report when practical, investigate it, coordinate a fix and disclosure window, and credit the reporter if requested. Response times are best-effort while the project is maintained by a small early-stage team.

## Security boundaries

In-process .NET plugins execute with the application user’s permissions and must be treated as trusted code. They are not a security sandbox. Future Python and untrusted extensions should run out of process with explicit permissions, bounded resources, and validated message contracts.

Project files, JSON payloads, CSV files, and plugins are untrusted inputs. Implementations must validate sizes, schemas, paths, numeric ranges, and plugin identities before use. Secrets must never be stored in project files, samples, logs, or source control.
