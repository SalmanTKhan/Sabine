-- Mail persistence. Replaces the in-memory `MailService` queue with a
-- DB-backed inbox so messages survive restarts and the receiver can
-- pick up attachments after logging in. Mirrors the classic-era
-- single-attachment shape from eAthena's `mail.c`.

CREATE TABLE `mail_messages` (
	`mailId` int(11) NOT NULL AUTO_INCREMENT,
	`senderId` int(11) NOT NULL,
	`senderName` varchar(24) NOT NULL,
	`receiverId` int(11) NOT NULL,
	`title` varchar(40) NOT NULL,
	`body` varchar(200) NOT NULL,
	`zeny` int(11) NOT NULL DEFAULT 0,
	`itemClassId` int(11) NOT NULL DEFAULT 0,
	`itemAmount` smallint(6) NOT NULL DEFAULT 0,
	`itemRefine` tinyint(4) NOT NULL DEFAULT 0,
	`itemIdentified` tinyint(1) NOT NULL DEFAULT 1,
	`itemDamaged` tinyint(1) NOT NULL DEFAULT 0,
	`read` tinyint(1) NOT NULL DEFAULT 0,
	`attachmentTaken` tinyint(1) NOT NULL DEFAULT 0,
	`sentAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
	PRIMARY KEY (`mailId`),
	KEY `idx_receiver` (`receiverId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;
