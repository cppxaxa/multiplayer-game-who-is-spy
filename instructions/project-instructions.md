# Description

* It is an azure webapp project
* src/iac folder should contain the infrastructure as code ARM template json
* Project should be easily openable in VS Code
* The azure webapp itself hosts the frontend UX
* The azure webapp also exposes secure APIs for UX
* Backend code is written directly in the src/ folder
    * src/lib contains src/lib/{task} folder. It identifies what task it does, and related code is present under it.
    * Task folders: `room`, `player`, `round`, `vote`, `state`, `shared`
        * `round` owns the full round lifecycle: game start, secret word assignment, turn timer, and phase advancement
        * `state` exposes game-state snapshot endpoints that the frontend polls for real-time updates
        * `shared` contains cross-cutting types, interfaces, enums, and table entity models used by other task folders — task folders only import from `shared`, never from each other
    * Use minimal APIs
    * Follow modularity and maintainability rules in `instructions/code-style.md` — thin endpoints, interface-driven services, no magic values, fail loudly at startup
    * Lock with Admin login - set a password from environment variable `AdminPass` to match and create room
    * Room joiners can simply use room link
* Frontend code is written in src/frontend
* Table schema are present in src/schema

# Infrastructure

* Azure Storage Table
* Azure webapp
    * Has System Managed Identity enabled
    * Has full access to Azure Storage Table

# Project startup

* It should check if all tables are present with the correct schema
* If the schema is wrong, return failure in all the public APIs as response
* Azure webapp startup must not fail
* If the tables are missing completely, create the tables, and refer schema from src/schema
