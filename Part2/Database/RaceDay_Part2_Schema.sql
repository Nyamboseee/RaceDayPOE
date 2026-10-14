-- RaceDay Part 2 database script
-- Rebuilds RaceDayDB from scratch with the Part 2 changes included:
-- Event gets Distance and EventType, Organizer and Participant get Email and PasswordHash

USE master;
GO

IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

CREATE TABLE Organizer (
    OrganizerID INT IDENTITY(1,1) PRIMARY KEY,
    FullName VARCHAR(100) NOT NULL,
    Role VARCHAR(50),
    ContactNumber VARCHAR(20),
    Email VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL
);
GO

CREATE TABLE Event (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganizerID INT NOT NULL FOREIGN KEY REFERENCES Organizer(OrganizerID),
    EventDate DATETIME NOT NULL,
    EventName VARCHAR(150) NOT NULL,
    Location VARCHAR(200) NOT NULL,
    Description VARCHAR(1000),
    Distance DECIMAL(6,2) NOT NULL DEFAULT 0,
    EventType VARCHAR(20) NOT NULL DEFAULT 'Run' CHECK (EventType IN ('Run', 'Walk', 'Cycle'))
);
GO

CREATE TABLE Category (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL FOREIGN KEY REFERENCES Event(EventID),
    CategoryName VARCHAR(100) NOT NULL,
    Distance DECIMAL(6,2) NOT NULL,
    MaxAge INT,
    MinAge INT
);
GO

CREATE TABLE Participant (
    ParticipantID INT IDENTITY(1,1) PRIMARY KEY,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Phone VARCHAR(20),
    Gender VARCHAR(20),
    DateOfBirth DATE,
    Email VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL
);
GO

CREATE TABLE Enrollment (
    EnrollmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL FOREIGN KEY REFERENCES Participant(ParticipantID),
    EventID INT NOT NULL FOREIGN KEY REFERENCES Event(EventID),
    CategoryID INT NOT NULL FOREIGN KEY REFERENCES Category(CategoryID),
    EnrollmentDate DATETIME DEFAULT GETDATE(),
    BibNumber VARCHAR(20) NOT NULL,
    UNIQUE (ParticipantID, CategoryID)
);
GO

CREATE TABLE Result (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrollmentID INT NOT NULL UNIQUE FOREIGN KEY REFERENCES Enrollment(EnrollmentID),
    FinishTime TIME,
    Position INT,
    Status VARCHAR(20) DEFAULT 'Finished'
);
GO

-- Sample data (seed accounts have a placeholder hash, so they cannot log in)
INSERT INTO Organizer (FullName, Role, ContactNumber, Email, PasswordHash) VALUES
('Thabo Nkosi', 'Head Organizer', '0821234567', 'thabo.nkosi@raceday.co.za', 'SEED_ACCOUNT_NO_LOGIN'),
('Lindiwe Zulu', 'Event Coordinator', '0837654321', 'lindiwe.zulu@raceday.co.za', 'SEED_ACCOUNT_NO_LOGIN');
GO

INSERT INTO Participant (FirstName, LastName, Phone, Gender, DateOfBirth, Email, PasswordHash) VALUES
('Sipho', 'Mokoena', '0731112222', 'Male', '1995-04-12', 'sipho.mokoena@example.com', 'SEED_ACCOUNT_NO_LOGIN'),
('Aisha', 'Patel', '0749998888', 'Female', '1998-09-23', 'aisha.patel@example.com', 'SEED_ACCOUNT_NO_LOGIN');
GO

INSERT INTO Event (OrganizerID, EventDate, EventName, Location, Description, Distance, EventType) VALUES
(1, '2026-11-14 06:00', 'Durban Beachfront Marathon', 'North Beach, Durban', 'Scenic coastal marathon along the Durban beachfront.', 42.20, 'Run'),
(1, '2026-10-03 06:30', 'Comrades Community Fun Run', 'Pietermaritzburg CBD', 'Community fun run inspired by the Comrades Marathon route.', 10.00, 'Run'),
(2, '2026-09-20 07:00', 'Cape Town Cycle Challenge', 'Sea Point Promenade, Cape Town', 'Road cycling event around the Cape Peninsula.', 100.00, 'Cycle');
GO

INSERT INTO Category (EventID, CategoryName, Distance, MaxAge, MinAge) VALUES
(1, '10km Fun Run', 10.00, 65, 12),
(1, 'Half Marathon', 21.10, 60, 18),
(2, '5km Family Walk', 5.00, 99, 6),
(2, '10km Run', 10.00, 65, 12),
(3, '50km Cycle Race', 50.00, 60, 16),
(3, '100km Cycle Race', 100.00, 55, 18);
GO

INSERT INTO Enrollment (ParticipantID, EventID, CategoryID, BibNumber) VALUES
(1, 1, 1, 'RD-1001'),
(1, 3, 5, 'RD-1002'),
(2, 1, 2, 'RD-1003'),
(2, 2, 3, 'RD-1004');
GO

INSERT INTO Result (EnrollmentID, FinishTime, Position, Status) VALUES
(1, '00:52:14', 12, 'Finished'),
(3, '00:28:40', 5, 'Finished');
GO

