USE [LevelMovement];
SET NOCOUNT ON;
SET XACT_ABORT ON;

-- Preview first. Set to 1 to permanently remove the listed obsolete demo/test rows.
DECLARE @ApplyCleanup bit = 0;

DECLARE @Targets TABLE
(
    RegistrationNumber nvarchar(20) PRIMARY KEY,
    ExpectedDeleted bit NOT NULL,
    Reason nvarchar(120) NOT NULL
);

INSERT INTO @Targets (RegistrationNumber, ExpectedDeleted, Reason)
VALUES
    (N'XCV334GP', 0, N'Invalid model year 2029; removal requested by owner'),
    (N'E2E-MVEZFR5N', 1, N'Completed browser test'),
    (N'E2E-MVEZH6O1', 1, N'Completed browser test'),
    (N'E2E-MVEZIPZM', 1, N'Completed browser test'),
    (N'RACE-MVEZG0QV', 1, N'Completed concurrency test'),
    (N'RACE-MVEZHGKD', 1, N'Completed concurrency test'),
    (N'RACE-MVEZIZDB', 1, N'Completed concurrency test'),
    (N'E2E-MVF04MP4', 1, N'Completed final browser verification'),
    (N'RACE-MVF04PLI', 1, N'Completed final concurrency verification');

BEGIN TRY
    BEGIN TRANSACTION;

    -- Capture and lock only the approved rows. A corrected/restored row is skipped.
    DECLARE @Candidates TABLE (Id uniqueidentifier PRIMARY KEY);
    INSERT INTO @Candidates (Id)
    SELECT vehicle.Id
    FROM dbo.Vehicles AS vehicle WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN @Targets AS target ON target.RegistrationNumber = vehicle.RegistrationNumber
    WHERE vehicle.IsDeleted = target.ExpectedDeleted
      AND (vehicle.RegistrationNumber <> N'XCV334GP'
           OR (vehicle.Make = N'Ford' AND vehicle.Model = N'Ranger' AND vehicle.Year = 2029));

    SELECT vehicle.*, target.Reason
    FROM dbo.Vehicles AS vehicle
    INNER JOIN @Candidates AS candidate ON candidate.Id = vehicle.Id
    INNER JOIN @Targets AS target ON target.RegistrationNumber = vehicle.RegistrationNumber
    ORDER BY vehicle.RegistrationNumber;

    IF @ApplyCleanup = 1
    BEGIN
        DELETE vehicle
        OUTPUT deleted.RegistrationNumber AS RemovedRegistration
        FROM dbo.Vehicles AS vehicle
        INNER JOIN @Candidates AS candidate ON candidate.Id = vehicle.Id;

        SELECT @@ROWCOUNT AS RemovedRows;
    END
    ELSE
        SELECT COUNT(*) AS PreviewRows FROM @Candidates;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

-- Read-only validation of the actual seed catalog; valid seeds are never targeted above.
SELECT RegistrationNumber, Make, Model, Year, DailyRate, IsDeleted
FROM dbo.Vehicles
WHERE RegistrationNumber LIKE N'DEMO[0-9][0-9][0-9]GP'
  AND TRY_CONVERT(int, SUBSTRING(RegistrationNumber, 5, 3)) BETWEEN 1 AND 50
  AND (Year NOT BETWEEN 1000 AND 2026
       OR DailyRate <= 0
       OR LEN(LTRIM(RTRIM(Make))) = 0
       OR LEN(LTRIM(RTRIM(Model))) = 0
       OR CreatedDate > SYSUTCDATETIME()
       OR ConcurrencyStamp = '00000000-0000-0000-0000-000000000000');

SELECT COUNT(*) AS SeedRecords,
       COALESCE(SUM(CASE WHEN IsDeleted = 0 THEN 1 ELSE 0 END), 0) AS ActiveSeedRecords
FROM dbo.Vehicles
WHERE RegistrationNumber LIKE N'DEMO[0-9][0-9][0-9]GP'
  AND TRY_CONVERT(int, SUBSTRING(RegistrationNumber, 5, 3)) BETWEEN 1 AND 50;
