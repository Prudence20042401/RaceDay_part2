-- ============================================================
-- Database: RaceDay
-- Description: Full database schema for RaceDay event management system

-- ============================================================
-- DROP TABLES (in correct order to avoid FK conflicts)
-- ============================================================
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.EventImages', 'U') IS NOT NULL DROP TABLE dbo.EventImages;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Organisers', 'U') IS NOT NULL DROP TABLE dbo.Organisers;
IF OBJECT_ID('dbo.Participants', 'U') IS NOT NULL DROP TABLE dbo.Participants;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;

-- ============================================================
-- CREATE TABLES
-- ============================================================

-- Users Table
CREATE TABLE dbo.Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Role NVARCHAR(20) NOT NULL CHECK (Role IN ('Organiser', 'Participant')),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);

-- Organisers Table
CREATE TABLE dbo.Organisers (
    OrganiserID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL UNIQUE,
    OrganisationName NVARCHAR(200) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Organisers_Users FOREIGN KEY (UserID) 
        REFERENCES dbo.Users(UserID) ON DELETE CASCADE
);

-- Participants Table
CREATE TABLE dbo.Participants (
    ParticipantID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL UNIQUE,
    DateOfBirth DATE NOT NULL,
    ProfilePictureURL NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Participants_Users FOREIGN KEY (UserID) 
        REFERENCES dbo.Users(UserID) ON DELETE CASCADE
);

-- Events Table
CREATE TABLE dbo.Events (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    EventDate DATE NOT NULL,
    Location NVARCHAR(300) NOT NULL,
    Distance DECIMAL(10,2) NOT NULL,
    EventType NVARCHAR(20) NOT NULL CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Events_Organisers FOREIGN KEY (OrganiserID) 
        REFERENCES dbo.Organisers(OrganiserID) ON DELETE CASCADE
);

-- EventImages Table
CREATE TABLE dbo.EventImages (
    ImageID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    ImageURL NVARCHAR(500) NOT NULL,
    IsMain BIT NOT NULL DEFAULT 0,
    UploadedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_EventImages_Events FOREIGN KEY (EventID) 
        REFERENCES dbo.Events(EventID) ON DELETE CASCADE
);

-- Categories Table
CREATE TABLE dbo.Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(300) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID) 
        REFERENCES dbo.Events(EventID) ON DELETE CASCADE
);

-- Enrolments Table - FIXED
CREATE TABLE dbo.Enrolments (
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    EventID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled', 'Completed')),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Enrolments_Participants FOREIGN KEY (ParticipantID) 
        REFERENCES dbo.Participants(ParticipantID) ON DELETE CASCADE,
    CONSTRAINT FK_Enrolments_Events FOREIGN KEY (EventID) 
        REFERENCES dbo.Events(EventID) ON DELETE NO ACTION,  -- FIXED: Changed from CASCADE
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID) 
        REFERENCES dbo.Categories(CategoryID) ON DELETE NO ACTION,  -- FIXED: Changed from CASCADE
    CONSTRAINT UQ_Enrolments_Participant_Event UNIQUE (ParticipantID, EventID)
);

-- Results Table
CREATE TABLE dbo.Results (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    FinishTime TIME NULL,
    FinishingPosition INT NULL,
    RecordedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID) 
        REFERENCES dbo.Enrolments(EnrolmentID) ON DELETE CASCADE,
    CONSTRAINT CHK_Result_Values CHECK (
        (FinishTime IS NOT NULL AND FinishingPosition IS NOT NULL) OR 
        (FinishTime IS NULL AND FinishingPosition IS NULL)
    )
);

-- ============================================================
-- CREATE INDEXES FOR PERFORMANCE
-- ============================================================
CREATE INDEX IX_Users_Email ON dbo.Users(Email);
CREATE INDEX IX_Events_OrganiserID ON dbo.Events(OrganiserID);
CREATE INDEX IX_Events_EventDate ON dbo.Events(EventDate);
CREATE INDEX IX_Categories_EventID ON dbo.Categories(EventID);
CREATE INDEX IX_Enrolments_ParticipantID ON dbo.Enrolments(ParticipantID);
CREATE INDEX IX_Enrolments_EventID ON dbo.Enrolments(EventID);
CREATE INDEX IX_Enrolments_CategoryID ON dbo.Enrolments(CategoryID);
CREATE INDEX IX_Enrolments_Status ON dbo.Enrolments(Status);
CREATE INDEX IX_EventImages_EventID ON dbo.EventImages(EventID);

-- ============================================================
-- SEED DATA
-- ============================================================

-- Insert Users
INSERT INTO dbo.Users (Email, PasswordHash, FirstName, LastName, Role, CreatedAt)
VALUES 
    ('john.organiser@email.com', 
     'Johndlamini@12345!',  
     'John', 'Dlamini', 'Organiser', GETUTCDATE()),
    ('sarah.organiser@email.com', 
      'SarahVanerMerwe@12345!',  
     'Sarah', 'Van der Merwe', 'Organiser', GETUTCDATE()),
    ('thabo.participant@email.com', 
     'Thabomokoena@12345!',  
     'Thabo', 'Mokoena', 'Participant', GETUTCDATE()),
    ('linda.participant@email.com', 
       'Lindanaidoo@12345!', 
     'Linda', 'Naidoo', 'Participant', GETUTCDATE());

-- Insert Organisers
INSERT INTO dbo.Organisers (UserID, OrganisationName, PhoneNumber, CreatedAt)
SELECT 
    UserID,
    CASE 
        WHEN Email = 'john.organiser@email.com' THEN 'Durban Running Club'
        WHEN Email = 'sarah.organiser@email.com' THEN 'Cape Town Cycle Events'
    END,
    CASE 
        WHEN Email = 'john.organiser@email.com' THEN '+27 82 123 4567'
        WHEN Email = 'sarah.organiser@email.com' THEN '+27 83 234 5678'
    END,
    GETUTCDATE()
FROM dbo.Users
WHERE Role = 'Organiser';

-- Insert Participants
INSERT INTO dbo.Participants (UserID, DateOfBirth, ProfilePictureURL, CreatedAt)
SELECT 
    UserID,
    CASE 
        WHEN Email = 'thabo.participant@email.com' THEN '1990-05-15'
        WHEN Email = 'linda.participant@email.com' THEN '1988-11-22'
    END,
    NULL,
    GETUTCDATE()
FROM dbo.Users
WHERE Role = 'Participant';

-- Insert Events
DECLARE @JohnOrganiserID INT = (SELECT OrganiserID FROM dbo.Organisers WHERE UserID = (SELECT UserID FROM dbo.Users WHERE Email = 'john.organiser@email.com'));
DECLARE @SarahOrganiserID INT = (SELECT OrganiserID FROM dbo.Organisers WHERE UserID = (SELECT UserID FROM dbo.Users WHERE Email = 'sarah.organiser@email.com'));

INSERT INTO dbo.Events (OrganiserID, Name, Description, EventDate, Location, Distance, EventType, CreatedAt)
VALUES 
    (@JohnOrganiserID, 'Durban Summer Run 2026', 
     'Annual summer road run along the Durban beachfront. A flat, fast course perfect for setting personal bests.', 
     '2026-09-15', 'Durban Beachfront, Durban', 10.00, 'Run', GETUTCDATE()),
    (@JohnOrganiserID, 'Pietermaritzburg Park Walk', 
     'A scenic community walk through the botanical gardens and parks of Pietermaritzburg.', 
     '2026-08-20', 'Botanical Gardens, Pietermaritzburg', 5.00, 'Walk', GETUTCDATE()),
    (@SarahOrganiserID, 'Cape Town Cycle Tour 2026', 
     'The worlds largest timed cycle event. Experience the breathtaking views of the Cape Peninsula.', 
     '2026-10-02', 'Cape Town Stadium, Cape Town', 109.00, 'Cycle', GETUTCDATE());

-- Insert Event Images
DECLARE @Event1ID INT = (SELECT EventID FROM dbo.Events WHERE Name = 'Durban Summer Run 2026');
DECLARE @Event2ID INT = (SELECT EventID FROM dbo.Events WHERE Name = 'Pietermaritzburg Park Walk');
DECLARE @Event3ID INT = (SELECT EventID FROM dbo.Events WHERE Name = 'Cape Town Cycle Tour 2026');

INSERT INTO dbo.EventImages (EventID, ImageURL, IsMain, UploadedAt)
VALUES 
    (@Event1ID, 'https://runteststorage.blob.core.windows.net/eventimages/durban-summer-run-banner.jpg', 1, GETUTCDATE()),
    (@Event2ID, 'https://runteststorage.blob.core.windows.net/eventimages/pmb-park-walk-banner.jpg', 1, GETUTCDATE()),
    (@Event3ID, 'https://runteststorage.blob.core.windows.net/eventimages/ct-cycle-tour-banner.jpg', 1, GETUTCDATE());

-- Insert Categories
INSERT INTO dbo.Categories (EventID, Name, Description, CreatedAt)
VALUES 
    (@Event1ID, 'Under 20', 'Open category for runners under 20 years old', GETUTCDATE()),
    (@Event1ID, 'Senior 20-39', 'Open category for runners aged 20 to 39', GETUTCDATE()),
    (@Event1ID, 'Masters 40+', 'Open category for runners aged 40 and above', GETUTCDATE()),
    (@Event2ID, 'Family Walk', 'Open category for families with children', GETUTCDATE()),
    (@Event2ID, 'Individual Walk', 'Open category for individual walkers', GETUTCDATE()),
    (@Event3ID, 'Elite', 'Competitive category for elite cyclists', GETUTCDATE()),
    (@Event3ID, 'Amateur', 'Open category for amateur cyclists', GETUTCDATE()),
    (@Event3ID, 'Recreational', 'Recreational category for casual cyclists', GETUTCDATE());

-- Insert Enrolments
DECLARE @ThaboParticipantID INT = (SELECT ParticipantID FROM dbo.Participants WHERE UserID = (SELECT UserID FROM dbo.Users WHERE Email = 'thabo.participant@email.com'));
DECLARE @LindaParticipantID INT = (SELECT ParticipantID FROM dbo.Participants WHERE UserID = (SELECT UserID FROM dbo.Users WHERE Email = 'linda.participant@email.com'));

DECLARE @SeniorCategoryID INT = (SELECT CategoryID FROM dbo.Categories WHERE EventID = @Event1ID AND Name = 'Senior 20-39');
DECLARE @MastersCategoryID INT = (SELECT CategoryID FROM dbo.Categories WHERE EventID = @Event1ID AND Name = 'Masters 40+');
DECLARE @IndividualWalkCategoryID INT = (SELECT CategoryID FROM dbo.Categories WHERE EventID = @Event2ID AND Name = 'Individual Walk');
DECLARE @FamilyWalkCategoryID INT = (SELECT CategoryID FROM dbo.Categories WHERE EventID = @Event2ID AND Name = 'Family Walk');
DECLARE @AmateurCategoryID INT = (SELECT CategoryID FROM dbo.Categories WHERE EventID = @Event3ID AND Name = 'Amateur');

INSERT INTO dbo.Enrolments (ParticipantID, EventID, CategoryID, Status, EnrolmentDate)
VALUES 
    (@ThaboParticipantID, @Event1ID, @SeniorCategoryID, 'Confirmed', GETUTCDATE()),
    (@ThaboParticipantID, @Event2ID, @IndividualWalkCategoryID, 'Confirmed', GETUTCDATE()),
    (@ThaboParticipantID, @Event3ID, @AmateurCategoryID, 'Pending', GETUTCDATE()),
    (@LindaParticipantID, @Event1ID, @MastersCategoryID, 'Confirmed', GETUTCDATE()),
    (@LindaParticipantID, @Event2ID, @FamilyWalkCategoryID, 'Confirmed', GETUTCDATE());

-- Insert Results - FIXED
DECLARE @ThaboEnrolmentEvent1 INT = (SELECT EnrolmentID FROM dbo.Enrolments WHERE ParticipantID = @ThaboParticipantID AND EventID = @Event1ID);
DECLARE @ThaboEnrolmentEvent2 INT = (SELECT EnrolmentID FROM dbo.Enrolments WHERE ParticipantID = @ThaboParticipantID AND EventID = @Event2ID);
DECLARE @LindaEnrolmentEvent1 INT = (SELECT EnrolmentID FROM dbo.Enrolments WHERE ParticipantID = @LindaParticipantID AND EventID = @Event1ID);
DECLARE @LindaEnrolmentEvent2 INT = (SELECT EnrolmentID FROM dbo.Enrolments WHERE ParticipantID = @LindaParticipantID AND EventID = @Event2ID);

INSERT INTO dbo.Results (EnrolmentID, FinishTime, FinishingPosition, RecordedAt)
VALUES 
    (@ThaboEnrolmentEvent1, '00:47:30', 15, GETUTCDATE()),
    (@ThaboEnrolmentEvent2, '00:35:20', 8, GETUTCDATE()),
    (@LindaEnrolmentEvent1, '00:52:15', 28, GETUTCDATE()),
    (@LindaEnrolmentEvent2, '00:42:45', 12, GETUTCDATE());

-- ============================================================
-- Verification Queries (for testing purposes)
-- ============================================================
-- SELECT * FROM dbo.Users;
-- SELECT * FROM dbo.Organisers;
-- SELECT * FROM dbo.Participants;
-- SELECT * FROM dbo.Events;
-- SELECT * FROM dbo.EventImages;
-- SELECT * FROM dbo.Categories;
-- SELECT * FROM dbo.Enrolments;
-- SELECT * FROM dbo.Results;