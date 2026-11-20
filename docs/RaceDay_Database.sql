/* RaceDay - Part 1 SQL Database Script (SQL Server / SSMS)
   Run on a clean instance. Matches the ERD and the EF Core model. */
IF DB_ID('RaceDayDb_Part1') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDb_Part1 SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDb_Part1;
END
GO
CREATE DATABASE RaceDayDb_Part1;
GO
USE RaceDayDb_Part1;
GO

CREATE TABLE Roles (
    RoleId   INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(20) NOT NULL,
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
);

CREATE TABLE Users (
    UserId            INT IDENTITY(1,1) PRIMARY KEY,
    RoleId            INT NOT NULL,
    FirstName         NVARCHAR(50)  NOT NULL,
    LastName          NVARCHAR(50)  NOT NULL,
    Email             NVARCHAR(150) NOT NULL,
    PasswordHash      NVARCHAR(255) NOT NULL,
    PhoneNumber       NVARCHAR(20)  NULL,
    DateOfBirth       DATETIME2     NULL,
    ProfilePictureUrl NVARCHAR(500) NULL,
    CreatedAt         DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);

CREATE TABLE Events (
    EventId        INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId    INT NOT NULL,
    Name           NVARCHAR(150)  NOT NULL,
    Description    NVARCHAR(1000) NOT NULL,
    EventDate      DATETIME2      NOT NULL,
    Location       NVARCHAR(200)  NOT NULL,
    DistanceKm     DECIMAL(6,2)   NOT NULL,
    EventType      NVARCHAR(10)   NOT NULL,
    BannerImageUrl NVARCHAR(500)  NULL,
    CreatedAt      DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Events_Type CHECK (EventType IN ('Run','Walk','Cycle')),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserId) REFERENCES Users(UserId)
);

CREATE TABLE Categories (
    CategoryId   INT IDENTITY(1,1) PRIMARY KEY,
    EventId      INT NOT NULL,
    Name         NVARCHAR(100) NOT NULL,
    CategoryType NVARCHAR(10)  NOT NULL,
    MinAge       INT NULL,
    MaxAge       INT NULL,
    DistanceKm   DECIMAL(6,2) NULL,
    CONSTRAINT CK_Categories_Type CHECK (CategoryType IN ('Age','Distance')),
    CONSTRAINT UQ_Categories_Event_Name UNIQUE (EventId, Name),
    CONSTRAINT UQ_Categories_Event_Id UNIQUE (EventId, CategoryId), -- target of composite FK
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId) REFERENCES Events(EventId) ON DELETE CASCADE
);

CREATE TABLE Enrolments (
    EnrolmentId   INT IDENTITY(1,1) PRIMARY KEY,
    EventId       INT NOT NULL,
    CategoryId    INT NOT NULL,
    ParticipantId INT NOT NULL,
    EnrolmentDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Status        NVARCHAR(10) NOT NULL DEFAULT 'Pending',
    CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Pending','Confirmed','Cancelled')),
    CONSTRAINT UQ_Enrolments_Event_Participant UNIQUE (EventId, ParticipantId),
    CONSTRAINT FK_Enrolments_Events FOREIGN KEY (EventId) REFERENCES Events(EventId),
    CONSTRAINT FK_Enrolments_Users  FOREIGN KEY (ParticipantId) REFERENCES Users(UserId),
    -- category must belong to the same event
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (EventId, CategoryId) REFERENCES Categories(EventId, CategoryId)
);

CREATE TABLE Results (
    ResultId       INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId    INT NOT NULL,
    FinishTime     TIME NOT NULL,
    FinishPosition INT  NOT NULL,
    RecordedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Results_Enrolment UNIQUE (EnrolmentId),
    CONSTRAINT CK_Results_Position CHECK (FinishPosition > 0),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId)
);
GO

/* ---------- SEED DATA ---------- */
SET IDENTITY_INSERT Roles ON;
INSERT INTO Roles (RoleId, RoleName) VALUES (1,'Organiser'),(2,'Participant');
SET IDENTITY_INSERT Roles OFF;

-- NOTE: placeholder hashes only; real accounts are created through POST /api/auth/register
INSERT INTO Users (RoleId, FirstName, LastName, Email, PasswordHash, PhoneNumber, DateOfBirth) VALUES
(1,'Thabo','Mokoena','thabo@jozirun.co.za','HASH_PLACEHOLDER_1','0821234567','1985-03-12'),
(1,'Lerato','Naidoo','lerato@capecycle.co.za','HASH_PLACEHOLDER_2','0839876543','1988-07-25'),
(2,'Sipho','Dlamini','sipho@gmail.com','HASH_PLACEHOLDER_3','0715551234','1995-01-30'),
(2,'Aisha','Patel','aisha@gmail.com','HASH_PLACEHOLDER_4','0724449876','2001-09-14'),
(2,'Pieter','van Wyk','pieter@gmail.com','HASH_PLACEHOLDER_5',NULL,'1979-11-02');

INSERT INTO Events (OrganiserId, Name, Description, EventDate, Location, DistanceKm, EventType) VALUES
(1,'Joburg Spring 10km','A fast, flat 10km road run through the northern suburbs.','2027-03-14 06:30','Johannesburg, Gauteng',10.00,'Run'),
(1,'Soweto Community Walk','A friendly charity walk through Soweto.','2027-04-10 07:00','Soweto, Gauteng',5.00,'Walk'),
(2,'Peninsula Cycle Classic','A scenic 50km cycle around the Cape Peninsula.','2027-05-22 06:00','Cape Town, Western Cape',50.00,'Cycle');

INSERT INTO Categories (EventId, Name, CategoryType, MinAge, MaxAge, DistanceKm) VALUES
(1,'Under 20','Age',0,19,NULL),
(1,'Senior','Age',20,59,NULL),
(1,'Veteran 60+','Age',60,99,NULL),
(2,'5km Fun Walk','Distance',NULL,NULL,5.00),
(2,'Under 20','Age',0,19,NULL),
(3,'50km Open','Distance',NULL,NULL,50.00),
(3,'25km Social','Distance',NULL,NULL,25.00);

INSERT INTO Enrolments (EventId, CategoryId, ParticipantId, Status) VALUES
(1,2,3,'Confirmed'),
(1,2,4,'Confirmed'),
(1,2,5,'Pending'),
(2,4,3,'Confirmed'),
(3,6,5,'Confirmed');

INSERT INTO Results (EnrolmentId, FinishTime, FinishPosition) VALUES
(1,'00:46:12',12),
(2,'00:52:40',41);
GO

SELECT 'Roles' AS TableName, COUNT(*) AS Rows FROM Roles UNION ALL
SELECT 'Users',COUNT(*) FROM Users UNION ALL
SELECT 'Events',COUNT(*) FROM Events UNION ALL
SELECT 'Categories',COUNT(*) FROM Categories UNION ALL
SELECT 'Enrolments',COUNT(*) FROM Enrolments UNION ALL
SELECT 'Results',COUNT(*) FROM Results;

