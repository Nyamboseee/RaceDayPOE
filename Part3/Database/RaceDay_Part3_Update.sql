-- RaceDay Part 3 database update
-- Adds the three columns Part 3 needs. Safe to run more than once, and it keeps your existing data.

USE RaceDayDB;
GO

-- Enrolment status: new enrolments start as Pending and the organiser confirms them
IF COL_LENGTH('Enrollment', 'Status') IS NULL
BEGIN
    ALTER TABLE Enrollment
        ADD Status VARCHAR(20) NOT NULL
            CONSTRAINT DF_Enrollment_Status DEFAULT 'Pending'
            CONSTRAINT CK_Enrollment_Status CHECK (Status IN ('Pending', 'Confirmed'));
END
GO

-- Event banner image (the URL of the picture in Azure Blob Storage)
IF COL_LENGTH('Event', 'BannerUrl') IS NULL
BEGIN
    ALTER TABLE Event ADD BannerUrl VARCHAR(500) NULL;
END
GO

-- Participant profile picture (the URL of the picture in Azure Blob Storage)
IF COL_LENGTH('Participant', 'ProfilePictureUrl') IS NULL
BEGIN
    ALTER TABLE Participant ADD ProfilePictureUrl VARCHAR(500) NULL;
END
GO

-- Check it worked
SELECT TOP 5 EnrollmentID, BibNumber, Status FROM Enrollment;
GO
