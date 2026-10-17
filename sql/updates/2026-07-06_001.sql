ALTER TABLE `accounts`
	ADD COLUMN `email` varchar(255) NULL DEFAULT NULL AFTER `password`,
	ADD UNIQUE KEY `uniq_accounts_email` (`email`);

CREATE TABLE `password_reset_tokens` (
	`tokenId` int(11) NOT NULL AUTO_INCREMENT,
	`accountId` bigint(20) NOT NULL,
	`tokenHash` char(64) NOT NULL,
	`expiresAt` datetime NOT NULL,
	`usedAt` datetime NULL DEFAULT NULL,
	`createdAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
	PRIMARY KEY (`tokenId`),
	UNIQUE KEY `uniq_tokenHash` (`tokenHash`),
	KEY `accountId` (`accountId`),
	CONSTRAINT `password_reset_tokens_ibfk_1` FOREIGN KEY (`accountId`) REFERENCES `accounts` (`accountId`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
