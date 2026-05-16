-- Adds the "isReturned" flag to mail_messages so the return-to-sender
-- flow (CZ_REQ_MAIL_RETURN / ZC_ACK_MAIL_RETURN) can refuse to bounce
-- a mail that was already bounced once, preventing ping-pong loops.

ALTER TABLE `mail_messages`
	ADD COLUMN `isReturned` TINYINT(1) NOT NULL DEFAULT 0 AFTER `attachmentTaken`;
