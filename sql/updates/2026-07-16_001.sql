ALTER TABLE `items`
	ADD COLUMN `hireExpireDate` int(10) unsigned NOT NULL DEFAULT 0 AFTER `location`;
