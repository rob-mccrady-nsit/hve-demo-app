---
title: Stockroom Project Guidelines
description: Durable Copilot guidance for the Stockroom HVE learning workshop repository
---

# Project Guidelines

## Project Purpose

Stockroom is a Flask and SQLite inventory and sales demo used for HVE-augmented development learning. Treat feature narratives, personas, requirements, and process descriptions in workshop materials as exercises, not approved project requirements or team policy, unless the user confirms them. The current feature objective is not established.

## Architecture and Data

* Flask routes, application behavior, and direct SQLite queries are in `app.py`; there is no ORM or separate service layer.
* The UI uses server-rendered Jinja templates in `templates/` and CSS in `static/css/`.
* `init_db()` creates tables, but no database migration framework is present. Preserve existing database data when changing schema or storage behavior.
* `inventory.db` and the Docker Compose `inventory-data` volume hold runtime data. Do not delete, reset, or edit them casually.
* `docs/03-stockroom-hve-workshop.html` is workshop material, not an authoritative application specification. Change it only when the task includes workshop content.

## Build, Run, and Test

The README documents `docker compose up --build` and a local run using `pip install -r requirements.txt` followed by `python app.py`. See [README.md](../README.md) for setup details.

No automated tests or test command are checked in. The README mentions historical Flask test-client checks, and the workshop identifies pytest smoke tests as future setup work. Do not report tests as passing unless runnable tests are present and executed. No CI, lint, formatting, or other local quality gate is configured in this repository.

## Requirements and Delivery

The authoritative requirements source, issue tracker, branch strategy, pull request and review expectations, pre-push checks, and approval boundaries have not been established. The GitHub remote is owned by the project owner. Do not infer policy from workshop examples; ask the user when scope, delivery expectations, or required approval is unclear.

## Documentation

When behavior, architecture, APIs, configuration, operations, or developer workflow changes, update the relevant README or other authoritative project documentation. Keep workshop guidance separate from confirmed project policy.