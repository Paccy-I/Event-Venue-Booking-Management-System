USE event_venue_booking;

-- Run once on installations created with the earlier schema.sql.
ALTER TABLE bookings
	ADD COLUMN event_start_time TIME NOT NULL DEFAULT '09:00:00' AFTER event_date,
	ADD COLUMN duration_days INT NOT NULL DEFAULT 1 AFTER event_start_time;

ALTER TABLE bookings
	ADD CONSTRAINT chk_bookings_duration_days CHECK (duration_days BETWEEN 1 AND 30);
