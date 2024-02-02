# Job Application Tracker

This is a C# console project that reads all emails from a specified mailbox and then returns the jobs you have applied to and the analytics for them.

## Features

- Connects to an email server using POP3 or IMAP protocol
- Filters emails by subject and sender to identify job applications
- Parses email content and attachments to extract job details
- Stores job data in a local SQLite database
- Generates reports and charts on job application status, response rate, and feedback

## Requirements

- .NET Framework 4.7.2 or higher
- MailBee.NET Objects library for email handling
- System.Data.SQLite library for database access
- ScottPlot library for data visualization

## Installation

- Download or clone this repository
- Open the solution file in Visual Studio
- Restore the NuGet packages
- Build the project

## Configuration

- Edit the app.config file and provide the following settings:
    - `EmailServer`: the email server address (e.g. pop.gmail.com)
    - `EmailPort`: the email server port (e.g. 995 for POP3 over SSL)
    - `EmailUser`: the email account username
    - `EmailPassword`: the email account password
    - `EmailProtocol`: the email protocol to use (POP3 or IMAP)
    - `EmailFilter`: the email filter to apply (e.g. "[Subject] contains 'application' AND [From] contains 'noreply'")
- Optionally, you can also change the following settings:
    - `DatabaseFile`: the path to the SQLite database file
    - `ReportFile`: the path to the report file
    - `ChartFile`: the path to the chart file

## Usage

- Run the executable file from the bin folder
- The program will connect to the email server and download the matching emails
- The program will parse the emails and store the job data in the database
- The program will generate a report and a chart and save them to the specified files
- The program will display a summary of the results and exit

## License

This project is licensed under the MIT License - see the LICENSE file for details
