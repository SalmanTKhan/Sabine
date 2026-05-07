-- World-scope variable storage. Mirrors the shape of vars_account /
-- vars_character so the existing GetVars / SaveVars helpers can be
-- reused. World-scope is a singleton, so ownerId is always 0 — there
-- is no foreign key, only the singleton row set.
CREATE TABLE `vars_world` (
  `varId` bigint(20) NOT NULL AUTO_INCREMENT,
  `ownerId` bigint(20) NOT NULL,
  `name` varchar(128) NOT NULL,
  `type` char(2) NOT NULL,
  `value` mediumtext NOT NULL,
  PRIMARY KEY (`varId`),
  KEY `ownerId` (`ownerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci ROW_FORMAT=COMPACT;
