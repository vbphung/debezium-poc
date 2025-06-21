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

INSERT INTO entertainment.dinosaurs (name, period, length_meters, weight_tons)
VALUES ('Trex', 'Late Cretaceous', 12.3, 8.4),
    ('Triceratops', 'Late Cretaceous', 9.0, 6.1),
    ('Velociraptor', 'Late Cretaceous', 2.0, 0.015),
    ('Stegosaurus', 'Late Jurassic', 9.0, 5.0),
    ('Brachiosaurus', 'Late Jurassic', 22.5, 35.0);
