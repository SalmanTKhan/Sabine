-- Marriage state on characters: char-id of the partner the
-- character is currently married to, plus the wall-clock time of
-- the wedding. Both are nullable — a character with both columns
-- NULL is unmarried. Divorcing a couple sets both sides back to
-- NULL via the partner-id lookup.
ALTER TABLE `characters`
	ADD COLUMN `marriedToCharId` bigint(20) NULL DEFAULT NULL AFTER `weightMax`,
	ADD COLUMN `marriedAt` datetime NULL DEFAULT NULL AFTER `marriedToCharId`;
