CREATE DATABASE IF NOT EXISTS event_venue_booking
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE event_venue_booking;

CREATE TABLE IF NOT EXISTS users (
	id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
	full_name VARCHAR(120) NOT NULL,
	email VARCHAR(190) NOT NULL UNIQUE,
	password_salt CHAR(32) NOT NULL,
	password_hash CHAR(64) NOT NULL,
	role ENUM('Customer', 'Administrator') NOT NULL DEFAULT 'Customer',
	created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS venues (
	id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
	name VARCHAR(150) NOT NULL,
	location VARCHAR(200) NOT NULL,
	capacity INT NOT NULL,
	price_per_day DECIMAL(10,2) NOT NULL,
	description TEXT NOT NULL,
	is_available BOOLEAN NOT NULL DEFAULT TRUE,
	CHECK (capacity > 0),
	CHECK (price_per_day >= 0)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS bookings (
	id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
	customer_id INT NOT NULL,
	venue_id INT NOT NULL,
	event_date DATE NOT NULL,
	event_start_time TIME NOT NULL DEFAULT '09:00:00',
	duration_days INT NOT NULL DEFAULT 1,
	event_details VARCHAR(500) NOT NULL DEFAULT '',
	quoted_price DECIMAL(10,2) NOT NULL,
	status ENUM('Pending', 'Approved', 'Rejected') NOT NULL DEFAULT 'Pending',
	active_slot TINYINT GENERATED ALWAYS AS (
		CASE WHEN status IN ('Pending', 'Approved') THEN 1 ELSE NULL END
	) STORED,
	created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT fk_bookings_customer FOREIGN KEY (customer_id) REFERENCES users(id),
	CONSTRAINT fk_bookings_venue FOREIGN KEY (venue_id) REFERENCES venues(id),
	CONSTRAINT uq_active_venue_date UNIQUE (venue_id, event_date, active_slot),
	INDEX ix_bookings_customer (customer_id),
	CHECK (quoted_price >= 0),
	CHECK (duration_days BETWEEN 1 AND 30)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS payments (
	id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
	booking_id INT NOT NULL,
	amount DECIMAL(10,2) NOT NULL,
	payment_method VARCHAR(40) NOT NULL,
	paid_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT fk_payments_booking FOREIGN KEY (booking_id) REFERENCES bookings(id),
	CHECK (amount > 0),
	INDEX ix_payments_booking (booking_id)
) ENGINE=InnoDB;

-- Optional starter data. Customer accounts are created from the application.
INSERT INTO venues (name, location, capacity, price_per_day, description)
SELECT 'Sample Conference Hall', 'Downtown', 150, 500.00, 'A sample venue; edit or remove it after setup.'
WHERE NOT EXISTS (SELECT 1 FROM venues WHERE name = 'Sample Conference Hall');

-- After registering your account in the application, grant administrator access:
-- UPDATE users SET role = 'Administrator' WHERE email = 'your-email@example.com';
