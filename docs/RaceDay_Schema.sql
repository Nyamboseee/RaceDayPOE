-- RaceDay Database Script
-- Creates the RaceDay database and all 6 tables, then adds sample data

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

-- Organizer table
CREATE TABLE Organizer (
    OrganizerID INT IDENTITY(1,1) PRIMARY KEY,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(50),
    ContactNumber VARCHAR(20)
);
GO

-- Event table
CREATE TABLE Event (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganizerID INT NOT NULL FOREIGN KEY REFERENCES Organizer(OrganizerID),
    EventDate DATETIME NOT NULL,
    EventName VARCHAR(150) NOT NULL,
    Location VARCHAR(200) NOT NULL,
    Description VARCHAR(1000)
);
GO

-- Category table (each event has multiple race categories e.g. 10km, 21km)
CREATE TABLE Category (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL FOREIGN KEY REFERENCES Event(EventID),
    CategoryName VARCHAR(100) NOT NULL,
    Distance DECIMAL(6,2) NOT NULL,
    MaxAge INT,
    MinAge INT
);
GO

-- Participant table
CREATE TABLE Participant (
    ParticipantID INT IDENTITY(1,1) PRIMARY KEY,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Phone VARCHAR(20),
    Gender VARCHAR(20),
    DateOfBirth DATE
);
GO

-- Enrollment table (links Participant to an Event and Category)
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

-- Result table (one result per enrollment, added after the race)
CREATE TABLE Result (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrollmentID INT NOT NULL UNIQUE FOREIGN KEY REFERENCES Enrollment(EnrollmentID),
    FinishTime TIME,
    Position INT,
    Status VARCHAR(20) DEFAULT 'Finished'
);
GO

-- Sample data: 2 organizers
INSERT INTO Organizer (FullName, Email, PasswordHash, Role, ContactNumber) VALUES
('Thabo Nkosi', 'thabo.nkosi@raceday.co.za', 'HASH_PLACEHOLDER_1', 'Head Organizer', '0821234567'),
('Lindiwe Zulu', 'lindiwe.zulu@raceday.co.za', 'HASH_PLACEHOLDER_2', 'Event Coordinator', '0837654321');
GO

-- Sample data: 2 participants
INSERT INTO Participant (FirstName, LastName, Email, PasswordHash, Phone, Gender, DateOfBirth) VALUES
('Sipho', 'Mokoena', 'sipho.mokoena@example.com', 'HASH_PLACEHOLDER_3', '0731112222', 'Male', '1995-04-12'),
('Aisha', 'Patel', 'aisha.patel@example.com', 'HASH_PLACEHOLDER_4', '0749998888', 'Female', '1998-09-23');
GO

-- Sample data: 3 events
INSERT INTO Event (OrganizerID, EventDate, EventName, Location, Description) VALUES
(1, '2026-11-14 06:00', 'Durban Beachfront Marathon', 'North Beach, Durban', 'Scenic coastal marathon along the Durban beachfront.'),
(1, '2026-10-03 06:30', 'Comrades Community Fun Run', 'Pietermaritzburg CBD', 'Community fun run inspired by the Comrades Marathon route.'),
(2, '2026-09-20 07:00', 'Cape Town Cycle Challenge', 'Sea Point Promenade, Cape Town', 'Road cycling event around the Cape Peninsula.');
GO

-- Sample data: categories for each event
INSERT INTO Category (EventID, CategoryName, Distance, MaxAge, MinAge) VALUES
(1, '10km Fun Run', 10.00, 65, 12),
(1, 'Half Marathon', 21.10, 60, 18),
(2, '5km Family Walk', 5.00, 99, 6),
(2, '10km Run', 10.00, 65, 12),
(3, '50km Cycle Race', 50.00, 60, 16),
(3, '100km Cycle Race', 100.00, 55, 18);
GO

-- Sample data: enrollments
INSERT INTO Enrollment (ParticipantID, EventID, CategoryID, BibNumber) VALUES
(1, 1, 1, 'RD-1001'),
(1, 3, 5, 'RD-1002'),
(2, 1, 2, 'RD-1003'),
(2, 2, 3, 'RD-1004');
GO

-- Sample data: results for enrollments that already happened
INSERT INTO Result (EnrollmentID, FinishTime, Position, Status) VALUES
(1, '00:52:14', 12, 'Finished'),
(3, '00:28:40', 5, 'Finished');
GO
