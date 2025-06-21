IF NOT EXISTS (
    SELECT *
    FROM sys.schemas
    WHERE name = 'entertainment'
)
BEGIN EXEC('CREATE SCHEMA entertainment');

END;

CREATE TABLE entertainment.dinosaurs (
    id INT IDENTITY(1, 1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    period NVARCHAR(100),
    length_meters FLOAT,
    weight_tons FLOAT
);
