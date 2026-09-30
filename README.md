# Ski Center

## 1.0 System Requirements
- Visual Studio 2026
- Windows 11

### 1.1 NuGet Packages

> Note: Before installing requires NuGet packages please check that they are not already installed using "Manage NuGet packages for Solution"

**List of Requires NuGet packages**

- `coverlet.collector` (Should only be installed in `TestLayer`)
- `Microsoft.EntityFrameworkCore.SqlServer` (Should only be installed in `DataLayer`, `EntityLayer`. Transitive in `TestLayer`)
- `Microsoft.EntityFrameworkCore.Tools` (Should only be installed in `DataLayer` Ignore this if you are not planning on performing Migrations)
- `Microsoft.Extensions.DependencyInjection` (Should only be installed in `PresentationLayer`. Transitive in `DataLayer`, `EntityLayer`, `TestLayer`)
- `Microsoft.NET.Test.Sdk` (Should only be installed in `TestLayer`)
- `NUnit` (Should only be installed in `TestLayer`)
- `NUnit.Analyzers` (Should only be installed in `TestLayer`)
- `NUnit3TestAdapter` (Should only be installed in `TestLayer`)

#### 1.1.1 How to Check NuGet Packages
1. Open the solution in Visual Studio.
2. Right-click on the Solution node in the **Solution Explorer**.
3. Select **Manage NuGet Packages for Solution...** from the context menu.
4. Navigate to the **Installed** tab to view all currently installed packages for your project.

#### 1.1.2 How to Install Missing NuGet Packages
1. Open the **Manage NuGet Packages for Solution...** window as described above.
2. Click on the **Browse** tab.
3. Enter the exact name of the missing package in the search bar.
4. Select the correct package from the search results, check the project(s) on the right-hand panel where it should be installed, and click **Install**. 
5. Accept any license agreements if prompted.

## 2.0 How to Set up Project

To ensure a smooth collaborative development experience, follow these basic steps to set up the environment locally:

### 2.1 Building the Solution
1. Open the `.sln` file in Visual Studio.
> Note: This is only needed if there are problems with the `sln` file. Otherwise ignore this step. 
3. Set the appropriate startup project by right-clicking the main project in the Solution Explorer and selecting **Set as Startup Project**. (`PresentationLayer` is the default in this project)
4. Go to **Build > Build Solution** (or press `Ctrl+Shift+B`) to compile the project and automatically restore missing dependencies.

### 2.2 Database Configuration (Entity Framework) 

> Note: If you are not working with database and backend you can ignore this.

Since the project utilizes Entity Framework Core, ensure your local database is correctly configured before running the application:
1. Verify that your local database connection string is properly set up in `SkiCenterDbContext` or your local user secrets. 
2. Open the Package Manager Console (`Tools > NuGet Package Manager > Package Manager Console`).
3. Run the command `Update-Database` to apply any pending migrations to your local SQL Server i

## 3.0 Working with Git

- 1: Git Cheat Sheet created by Jonathan windell documents basic information regarding how to work with git such as creating your own branch and pull requests. Read more here [Git Cheat Sheet](https://docs.google.com/document/d/12JM5xx4rBn-FlOd_DbpXtm4HkJVdDqVqcSJbPjPXmWQ/edit?usp=drive_link)
- 2: Git 101 created by Jonathan Windell contains necessary information regarding how Git works and basic operations such as merges, rebasing, squashing. Read more here [Git 101](https://forgejo.jonathans-labb.org/JonathanWindell/Git-Documentation-101)
- 3: Official Git documentation can also be helpful when more advanced operations are required. Read more here [Official Git Documentation](https://git-scm.com/docs)

### 3.1 How to Clone Project using Visual Studio (Recommended)
1. Start Visual Studio 2026
2. In the right hand menu choose "Clone a Repository"
3. Either use https://github.com/JonathanWindell/SkiCenter-Group6.git in the "Repository location" or choose "Browse a Repository" from Github and choose `SkiCenter-Group6`
4. Click "Clone"

### 3.2 How to Clone Project using Command Prompt (Not recommended)
1. Go to `SkiCenter-Group6` repository
2. Click on `Code` and copy https://github.com/JonathanWindell/SkiCenter-Group6.git (Or just copy the link from here)
3. Run the following command using the project's remote URL: `git clone https://github.com/JonathanWindell/SkiCenter-Group6.git`

### 3.3 Creating a Branch
It is **VERY IMPORTANT** that you create your own branch to keep the main branch stable.
1. In Visual Studio click on `Git` in the menu bar.
2. In **Git** menu choose **Open in Command Prompt**
3. Enter the command `git checkout -b <My-Branch>`
4. Verify that Git has switched branch by writing `git branch`. If action has been performed correct you should see your created **branch** and **main** branch. The branch you are currently working on is highlighted with color and `*` to the left of branch name. 

> Note: Whenever the <> are used you should always remove them. They act only as placeholders. 

### 3.4 Creating a Pull Request
Creating a pull request is the next step to ensure that what you have worked on is integrated to `main`

1. Once your work is complete, stage and commit your changes locally, then push your branch to the remote repository: `git push origin <your-branch-name>`

> Note:
> 1. Command to stage `git add <file>` or `git add .` which adds every changed file.
> 2. Command to commit `git commit -m "<Commit message>"`. 

2. Go to the remote repository platform in your web browser.
3. Navigate to the "Pull Requests" section and click on "New Pull Request".
4. Select your newly pushed branch as the source and the `main` branch (or designated development branch) as the target.
5. Provide a clear title and a detailed description explaining what your changes do, assign reviewers, and submit the PR.
