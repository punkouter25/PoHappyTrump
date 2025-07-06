
1.0 General Principles & Governance
1.1 Source of Truth: The prd.md file is the definitive source for all product requirements. It must never be modified by the AI. If any rule in this protocol conflicts with a requirement in prd.md, the prd.md takes precedence.
1.2 Design Philosophy: Prioritize simplicity, functional correctness, and future expandability. Avoid premature optimization and adhere to SOLID principles and GoF patterns / Note in code comments with GoF design patterns are used
1.3 Always look at console and log.txt: When running apps be aware of debug info in output and if you see something is broken bring it up to user
2.0 Workflow & User Interaction
2.1 Step-Driven Execution:
If a steps.md file exists, strictly follow the high-level steps defined within it.
If steps.md does not exist, execute only the user's direct and immediate request. Do not create a steps.md file unless explicitly asked to do so as part of new project development.
2.2 Progress Tracking: When following steps.md, mark completed steps using the format: - [x] Step X: Description.
2.3 Confirmation & Suggestions: After successfully completing a step from steps.md or fulfilling a direct user request, present the success report and offer 5 relevant subsequent tasks to advance the project.
2.4 Failure Protocol: If a step fails (e.g., code does not compile, tests fail), stop immediately. Report the failure, provide the full content of log.txt, state the exact error message, and await user instructions.
2.5 File Cleanup: When encountering potentially unused files or code, list all potentially removable items in a single request and await user confirmation before deleting anything.
3.0 Solution & Code Structure
3.1 Naming and Root Directory: The solution name is derived from the prd.md Title and must be prefixed with Po (e.g., PoProjectName). All files must be contained within a root directory named after the solution.
3.2 Root Directory Structure: The following structure is mandatory. Test projects must be placed at the root level and named according to .NET best practices.
Generated code
      /PoProjectName/
├── .github/
│   └── workflows/
│       └── deploy.yml
├── .vscode/
│   ├── launch.json
│   └── tasks.json
├── AzuriteData/
├── PoProjectName.Api/
│   ├── appsettings.Development.json
│   ├── appsettings.json
│   └── PoProjectName.Api.csproj
├── PoProjectName.Client/
│   └── (Contains Blazor Client .csproj or Godot project files)
├── PoProjectName.ApiTests/
│   └── PoProjectName.ApiTests.csproj
├── PoProjectName.IntegrationTests/
│   └── PoProjectName.IntegrationTests.csproj
├── PoProjectName.UnitTests/
│   └── PoProjectName.UnitTests.csproj
├── .editorconfig
├── .gitignore
├── PoProjectName.sln
├── log.txt
├── prd.md
├── README.md
└── steps.md
Include Domain/Application/Infrastructure as well if using onion architecture
Put AzuriteData in its own folder
4.0 Backend Development (C# / .NET API)
4.1 Framework & Architecture:
Target the .NET 9.x framework (or the latest stable version).
Default to Vertical Slice Architecture. If prd.md describes a highly complex domain, Onion Architecture may be used as well if it is best practices for the app
The chosen pattern must be justified with a comment in Program.cs.


4.2 Standards & Error Handling:
Use Dependency Injection for all services, registered in Program.cs with appropriate lifetimes.
Implement global exception handler middleware.
For calls to external services, implement the Circuit Breaker pattern using a library like Polly.


5.0 Azure Integration
5.1 Resource Groups & Keys: Shared resources reside in the PoShared resource group. The AI will use the Azure CLI to retrieve connection strings and will assume the user has an authenticated session.
5.2 Table Storage:
Local: Use the Azurite emulator.
Azure: Use the PoSharedTableStorage resource.
Table Naming Convention: [SolutionName][TableName] (e.g., PoSomeAppHighScores).


6.0 Frontend Development (UI)
6.1 Blazor WebAssembly:
Create a hosted Blazor WebAssembly project, served by the ASP.NET Core application.
Use the Radzen Blazor UI library for complex controls.


6.2 Godot .NET (Game Client):
Use Godot 4.x with C#. All project artifacts reside in the PoProjectName.Client/ folder.
Define static scene structure in .tscn files and implement logic in C# scripts.


7.0 Testing & Quality Assurance
7.1 Framework: Use xUnit for all tests.
7.2 Approach: For new features, create services and their corresponding integration tests first. Verify all tests pass before beginning UI implementation.
7.3 Test Categories: Create separate test projects for Unit, Integration, and API tests as defined in the solution structure (Section 3.2). Write tests for all business logic, data access, and external service connections.
8.0 Logging & Diagnostics 
8.1 Logging Strategy:
Server-Side: Implement a robust logging strategy that outputs simultaneously to the Console, Serilog (to file), and Application Insights. The default logging level must be Debug. 
Server Log File (log.txt): A single log.txt file must be created or overwritten in the root directory on each application run. It will contain only the detailed, timestamped logs from the server.
Client-Side: Client-side applications (Blazor, Godot) should write all diagnostic and debug information directly to their native console (e.g., the browser's developer console or the system console). Do not send client logs to the server.
Constantly add more details as needed to log.txt so that when the app is finished running the LLM coding AI can review it


8.2 Mandatory Diagnostics View:
A diagnostics view is mandatory for all applications with a UI (/diag route for Blazor, Diag.tscn for Godot).
This view must perform and display the real-time status of critical dependencies (Database, Backend API, etc.).
All diagnostic check results must be written to the server-side log targets.





