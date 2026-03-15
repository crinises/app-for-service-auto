CREATE DATABASE IF NOT EXISTS service_auto CHARACTER SET utf8 COLLATE utf8_romanian_ci;
USE service_auto;

DROP TABLE IF EXISTS Programari;
DROP TABLE IF EXISTS Automobile;
DROP TABLE IF EXISTS Clienti;

CREATE TABLE Clienti (
    ClientID INT AUTO_INCREMENT PRIMARY KEY,
    Nume VARCHAR(100) NOT NULL,
    Prenume VARCHAR(100) NOT NULL,
    Telefon VARCHAR(20) NOT NULL
);

CREATE TABLE Automobile (
    AutomobilID INT AUTO_INCREMENT PRIMARY KEY,
    ClientID INT NOT NULL,
    Marca VARCHAR(100) NOT NULL,
    Model VARCHAR(100) NOT NULL,
    NumarInmatriculare VARCHAR(20) NOT NULL,
    FOREIGN KEY (ClientID) REFERENCES Clienti(ClientID) ON DELETE CASCADE
);

CREATE TABLE Programari (
    ProgramareID INT AUTO_INCREMENT PRIMARY KEY,
    AutomobilID INT NOT NULL,
    DataProgramarii DATE NOT NULL,
    TipServiciu VARCHAR(200) NOT NULL,
    StatusProgramare VARCHAR(50) NOT NULL DEFAULT 'Programat',
    FOREIGN KEY (AutomobilID) REFERENCES Automobile(AutomobilID) ON DELETE CASCADE
);

INSERT INTO Clienti (Nume, Prenume, Telefon) VALUES
('Ionescu', 'Ion', '0721000001'),
('Popescu', 'Maria', '0732000002'),
('Dumitru', 'Andrei', '0743000003');

INSERT INTO Automobile (ClientID, Marca, Model, NumarInmatriculare) VALUES
(1, 'Dacia', 'Logan', 'IS 01 AAA'),
(2, 'Volkswagen', 'Golf', 'B 02 BBB'),
(3, 'Renault', 'Clio', 'CJ 03 CCC');

INSERT INTO Programari (AutomobilID, DataProgramarii, TipServiciu, StatusProgramare) VALUES
(1, DATE_ADD(CURDATE(), INTERVAL 3 DAY), 'Revizie', 'Programat'),
(2, DATE_ADD(CURDATE(), INTERVAL 5 DAY), 'Schimb ulei', 'Programat'),
(3, DATE_ADD(CURDATE(), INTERVAL 1 DAY), 'ITP', 'In lucru');
