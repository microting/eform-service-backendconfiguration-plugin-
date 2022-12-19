-- MariaDB dump 10.19  Distrib 10.6.11-MariaDB, for debian-linux-gnu (x86_64)
--
-- Host: localhost    Database: 420_eform-backend-configuration-plugin
-- ------------------------------------------------------
-- Server version	10.6.11-MariaDB-0ubuntu0.22.04.1

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `AreaInitialFieldVersions`
--

DROP TABLE IF EXISTS `AreaInitialFieldVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AreaInitialFieldVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `EformName` longtext DEFAULT NULL,
  `Notifications` tinyint(1) NOT NULL,
  `RepeatEvery` int(11) DEFAULT NULL,
  `RepeatType` int(11) DEFAULT NULL,
  `DayOfWeek` int(11) DEFAULT NULL,
  `Type` int(11) DEFAULT NULL,
  `Alarm` int(11) DEFAULT NULL,
  `EndDate` datetime(6) DEFAULT NULL,
  `AreaId` int(11) NOT NULL,
  `AreaInitialFieldId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `ComplianceEnabled` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `IX_AreaInitialFieldVersions_AreaId` (`AreaId`),
  CONSTRAINT `FK_AreaInitialFieldVersions_Areas_AreaId` FOREIGN KEY (`AreaId`) REFERENCES `Areas` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `AreaInitialFields`
--

DROP TABLE IF EXISTS `AreaInitialFields`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AreaInitialFields` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `EformName` longtext DEFAULT NULL,
  `Notifications` tinyint(1) NOT NULL,
  `RepeatEvery` int(11) DEFAULT NULL,
  `RepeatType` int(11) DEFAULT NULL,
  `DayOfWeek` int(11) DEFAULT NULL,
  `Type` int(11) DEFAULT NULL,
  `Alarm` int(11) DEFAULT NULL,
  `EndDate` datetime(6) DEFAULT NULL,
  `AreaId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `ComplianceEnabled` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_AreaInitialFields_AreaId` (`AreaId`),
  CONSTRAINT `FK_AreaInitialFields_Areas_AreaId` FOREIGN KEY (`AreaId`) REFERENCES `Areas` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=28 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `AreaProperties`
--

DROP TABLE IF EXISTS `AreaProperties`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AreaProperties` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `PropertyId` int(11) NOT NULL,
  `AreaId` int(11) NOT NULL,
  `Checked` tinyint(1) NOT NULL,
  `GroupMicrotingUuid` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AreaProperties_AreaId` (`AreaId`),
  KEY `IX_AreaProperties_PropertyId` (`PropertyId`),
  CONSTRAINT `FK_AreaProperties_Areas_AreaId` FOREIGN KEY (`AreaId`) REFERENCES `Areas` (`Id`),
  CONSTRAINT `FK_AreaProperties_Properties_PropertyId` FOREIGN KEY (`PropertyId`) REFERENCES `Properties` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=24 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `AreaPropertyVersions`
--

DROP TABLE IF EXISTS `AreaPropertyVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AreaPropertyVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `AreaPropertyId` int(11) NOT NULL,
  `PropertyId` int(11) NOT NULL,
  `AreaId` int(11) NOT NULL,
  `Checked` tinyint(1) NOT NULL,
  `GroupMicrotingUuid` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=48 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `AreaRuleInitialFields`
--

DROP TABLE IF EXISTS `AreaRuleInitialFields`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AreaRuleInitialFields` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `EformName` longtext DEFAULT NULL,
  `Notifications` tinyint(1) NOT NULL,
  `RepeatEvery` int(11) DEFAULT NULL,
  `RepeatType` int(11) DEFAULT NULL,
  `DayOfWeek` int(11) DEFAULT NULL,
  `Type` int(11) DEFAULT NULL,
  `Alarm` int(11) DEFAULT NULL,
  `EndDate` datetime(6) DEFAULT NULL,
  `AreaRuleId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `ComplianceEnabled` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_AreaRuleInitialFields_AreaRuleId` (`AreaRuleId`),
  CONSTRAINT `FK_AreaRuleInitialFields_AreaRules_AreaRuleId` FOREIGN KEY (`AreaRuleId`) REFERENCES `AreaRules` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=25 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `AreaRulePlannings`
--

DROP TABLE IF EXISTS `AreaRulePlannings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AreaRulePlannings` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `StartDate` datetime(6) DEFAULT NULL,
  `EndDate` datetime(6) DEFAULT NULL,
  `DayOfWeek` int(11) NOT NULL,
  `DayOfMonth` int(11) NOT NULL,
  `RepeatEvery` int(11) DEFAULT NULL,
  `RepeatType` int(11) DEFAULT NULL,
  `Status` tinyint(1) NOT NULL,
  `SendNotifications` tinyint(1) NOT NULL,
  `Alarm` int(11) NOT NULL,
  `Type` int(11) NOT NULL,
  `AreaRuleId` int(11) NOT NULL,
  `ItemPlanningId` int(11) NOT NULL,
  `FolderId` int(11) NOT NULL,
  `HoursAndEnergyEnabled` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `PropertyId` int(11) NOT NULL DEFAULT 0,
  `AreaId` int(11) NOT NULL DEFAULT 0,
  `ComplianceEnabled` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `IX_AreaRulePlannings_AreaRuleId` (`AreaRuleId`),
  CONSTRAINT `FK_AreaRulePlannings_AreaRules_AreaRuleId` FOREIGN KEY (`AreaRuleId`) REFERENCES `AreaRules` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `AreaRuleTranslationVersions`
--

DROP TABLE IF EXISTS `AreaRuleTranslationVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AreaRuleTranslationVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `AreaRuleTranslationId` int(11) NOT NULL,
  `Name` longtext DEFAULT NULL,
  `LanguageId` int(11) NOT NULL,
  `AreaRuleId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=79 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `AreaRuleTranslations`
--

DROP TABLE IF EXISTS `AreaRuleTranslations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AreaRuleTranslations` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(250) DEFAULT NULL,
  `LanguageId` int(11) NOT NULL,
  `AreaRuleId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AreaRuleTranslations_AreaRuleId` (`AreaRuleId`),
  CONSTRAINT `FK_AreaRuleTranslations_AreaRules_AreaRuleId` FOREIGN KEY (`AreaRuleId`) REFERENCES `AreaRules` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=79 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `AreaRuleVersions`
--

DROP TABLE IF EXISTS `AreaRuleVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AreaRuleVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `AreaRuleId` int(11) NOT NULL,
  `AreaId` int(11) NOT NULL,
  `EformId` int(11) DEFAULT NULL,
  `EformName` longtext DEFAULT NULL,
  `FolderId` int(11) NOT NULL,
  `FolderName` longtext DEFAULT NULL,
  `Alarm` int(11) DEFAULT NULL,
  `Type` int(11) DEFAULT NULL,
  `ChecklistStable` tinyint(1) DEFAULT NULL,
  `TailBite` tinyint(1) DEFAULT NULL,
  `DayOfWeek` int(11) NOT NULL,
  `GroupItemId` int(11) NOT NULL,
  `PropertyId` int(11) NOT NULL,
  `IsDefault` tinyint(1) NOT NULL,
  `RepeatEvery` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `RepeatType` int(11) DEFAULT NULL,
  `ComplianceEnabled` tinyint(1) DEFAULT NULL,
  `ComplianceModifiable` tinyint(1) NOT NULL DEFAULT 0,
  `Notifications` tinyint(1) DEFAULT NULL,
  `NotificationsModifiable` tinyint(1) NOT NULL DEFAULT 0,
  `SecondaryeFormId` int(11) NOT NULL DEFAULT 0,
  `SecondaryeFormName` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=53 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `AreaRules`
--

DROP TABLE IF EXISTS `AreaRules`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AreaRules` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `AreaId` int(11) NOT NULL,
  `PropertyId` int(11) NOT NULL,
  `EformId` int(11) DEFAULT NULL,
  `EformName` longtext DEFAULT NULL,
  `FolderId` int(11) NOT NULL,
  `FolderName` longtext DEFAULT NULL,
  `Alarm` int(11) DEFAULT NULL,
  `Type` int(11) DEFAULT NULL,
  `ChecklistStable` tinyint(1) DEFAULT NULL,
  `TailBite` tinyint(1) DEFAULT NULL,
  `DayOfWeek` int(11) NOT NULL,
  `GroupItemId` int(11) NOT NULL,
  `IsDefault` tinyint(1) NOT NULL,
  `RepeatEvery` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `RepeatType` int(11) DEFAULT NULL,
  `ComplianceEnabled` tinyint(1) DEFAULT NULL,
  `ComplianceModifiable` tinyint(1) NOT NULL DEFAULT 0,
  `Notifications` tinyint(1) DEFAULT NULL,
  `NotificationsModifiable` tinyint(1) NOT NULL DEFAULT 0,
  `SecondaryeFormId` int(11) NOT NULL DEFAULT 0,
  `SecondaryeFormName` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AreaRules_AreaId` (`AreaId`),
  KEY `IX_AreaRules_PropertyId` (`PropertyId`),
  CONSTRAINT `FK_AreaRules_Areas_AreaId` FOREIGN KEY (`AreaId`) REFERENCES `Areas` (`Id`),
  CONSTRAINT `FK_AreaRules_Properties_PropertyId` FOREIGN KEY (`PropertyId`) REFERENCES `Properties` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=27 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `AreaRulesPlanningVersions`
--

DROP TABLE IF EXISTS `AreaRulesPlanningVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AreaRulesPlanningVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `AreaRulePlanningId` int(11) NOT NULL,
  `StartDate` datetime(6) DEFAULT NULL,
  `EndDate` datetime(6) DEFAULT NULL,
  `DayOfWeek` int(11) NOT NULL,
  `DayOfMonth` int(11) NOT NULL,
  `RepeatEvery` int(11) DEFAULT NULL,
  `RepeatType` int(11) DEFAULT NULL,
  `Status` tinyint(1) NOT NULL,
  `SendNotifications` tinyint(1) NOT NULL,
  `Alarm` int(11) NOT NULL,
  `Type` int(11) NOT NULL,
  `AreaRuleId` int(11) NOT NULL,
  `ItemPlanningId` int(11) NOT NULL,
  `FolderId` int(11) NOT NULL,
  `HoursAndEnergyEnabled` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `PropertyId` int(11) NOT NULL DEFAULT 0,
  `AreaId` int(11) NOT NULL DEFAULT 0,
  `ComplianceEnabled` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `AreaTranslationVersions`
--

DROP TABLE IF EXISTS `AreaTranslationVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AreaTranslationVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `AreaId` int(11) NOT NULL,
  `Name` longtext DEFAULT NULL,
  `Description` longtext DEFAULT NULL,
  `LanguageId` int(11) NOT NULL,
  `AreaTranslationId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `InfoBox` longtext DEFAULT NULL,
  `Placeholder` longtext DEFAULT NULL,
  `NewItemName` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `AreaTranslations`
--

DROP TABLE IF EXISTS `AreaTranslations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AreaTranslations` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `AreaId` int(11) NOT NULL,
  `Name` longtext DEFAULT NULL,
  `Description` longtext DEFAULT NULL,
  `LanguageId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `InfoBox` longtext DEFAULT NULL,
  `Placeholder` longtext DEFAULT NULL,
  `NewItemName` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AreaTranslations_AreaId` (`AreaId`),
  CONSTRAINT `FK_AreaTranslations_Areas_AreaId` FOREIGN KEY (`AreaId`) REFERENCES `Areas` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=82 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `AreaVersions`
--

DROP TABLE IF EXISTS `AreaVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `AreaVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Type` int(11) NOT NULL,
  `ItemPlanningTagId` int(11) NOT NULL,
  `AreaId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `IsFarm` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=28 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Areas`
--

DROP TABLE IF EXISTS `Areas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Areas` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Type` int(11) NOT NULL,
  `ItemPlanningTagId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `IsFarm` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=31 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ChemicalProductPropertieSites`
--

DROP TABLE IF EXISTS `ChemicalProductPropertieSites`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ChemicalProductPropertieSites` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ChemicalId` int(11) NOT NULL,
  `ProductId` int(11) NOT NULL,
  `PropertyId` int(11) NOT NULL,
  `SdkCaseId` int(11) NOT NULL,
  `SdkSiteId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `LanguageId` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ChemicalProductProperties`
--

DROP TABLE IF EXISTS `ChemicalProductProperties`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ChemicalProductProperties` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ChemicalId` int(11) NOT NULL,
  `ProductId` int(11) NOT NULL,
  `PropertyId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `SdkCaseId` int(11) NOT NULL DEFAULT 0,
  `Locations` longtext DEFAULT NULL,
  `LanguageId` int(11) NOT NULL DEFAULT 0,
  `SdkSiteId` int(11) NOT NULL DEFAULT 0,
  `ExpireDate` datetime(6) DEFAULT NULL,
  `LastFolderName` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ChemicalProductPropertyVersionSites`
--

DROP TABLE IF EXISTS `ChemicalProductPropertyVersionSites`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ChemicalProductPropertyVersionSites` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ChemicalId` int(11) NOT NULL,
  `ProductId` int(11) NOT NULL,
  `PropertyId` int(11) NOT NULL,
  `SdkCaseId` int(11) NOT NULL,
  `SdkSiteId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `LanguageId` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ChemicalProductPropertyVersions`
--

DROP TABLE IF EXISTS `ChemicalProductPropertyVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ChemicalProductPropertyVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ChemicalProductPropertyId` int(11) NOT NULL,
  `ChemicalId` int(11) NOT NULL,
  `ProductId` int(11) NOT NULL,
  `PropertyId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `SdkCaseId` int(11) NOT NULL DEFAULT 0,
  `Locations` longtext DEFAULT NULL,
  `LanguageId` int(11) NOT NULL DEFAULT 0,
  `SdkSiteId` int(11) NOT NULL DEFAULT 0,
  `ExpireDate` datetime(6) DEFAULT NULL,
  `LastFolderName` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ComplianceVersions`
--

DROP TABLE IF EXISTS `ComplianceVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ComplianceVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ItemName` longtext DEFAULT NULL,
  `AreaId` int(11) NOT NULL DEFAULT 0,
  `AreaName` longtext DEFAULT NULL,
  `PlanningId` int(11) NOT NULL,
  `PropertyId` int(11) NOT NULL,
  `Deadline` datetime(6) NOT NULL,
  `StartDate` datetime(6) NOT NULL,
  `ComplianceId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `MicrotingSdkCaseId` int(11) NOT NULL DEFAULT 0,
  `MicrotingSdkeFormId` int(11) NOT NULL DEFAULT 0,
  `CheckListSiteId` int(11) NOT NULL DEFAULT 0,
  `PlanningCaseSiteId` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Compliances`
--

DROP TABLE IF EXISTS `Compliances`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Compliances` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ItemName` longtext DEFAULT NULL,
  `AreaId` int(11) NOT NULL DEFAULT 0,
  `AreaName` longtext DEFAULT NULL,
  `PlanningId` int(11) NOT NULL,
  `PropertyId` int(11) NOT NULL,
  `Deadline` datetime(6) NOT NULL,
  `StartDate` datetime(6) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `MicrotingSdkCaseId` int(11) NOT NULL DEFAULT 0,
  `MicrotingSdkeFormId` int(11) NOT NULL DEFAULT 0,
  `CheckListSiteId` int(11) NOT NULL DEFAULT 0,
  `PlanningCaseSiteId` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `EmailAttachmentVersions`
--

DROP TABLE IF EXISTS `EmailAttachmentVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `EmailAttachmentVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `EmailAttachmentId` int(11) NOT NULL,
  `EmailId` int(11) NOT NULL,
  `ResourceName` longtext DEFAULT NULL,
  `CidName` longtext DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `EmailAttachments`
--

DROP TABLE IF EXISTS `EmailAttachments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `EmailAttachments` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `EmailId` int(11) NOT NULL,
  `ResourceName` longtext DEFAULT NULL,
  `CidName` longtext DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_EmailAttachments_EmailId` (`EmailId`),
  CONSTRAINT `FK_EmailAttachments_Emails_EmailId` FOREIGN KEY (`EmailId`) REFERENCES `Emails` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `EmailVersions`
--

DROP TABLE IF EXISTS `EmailVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `EmailVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `To` longtext DEFAULT NULL,
  `From` longtext DEFAULT NULL,
  `Subject` longtext DEFAULT NULL,
  `Body` longtext DEFAULT NULL,
  `BodyType` longtext DEFAULT NULL,
  `Status` longtext DEFAULT NULL,
  `Error` longtext DEFAULT NULL,
  `Sent` longtext DEFAULT NULL,
  `SentAt` datetime(6) NOT NULL,
  `Version` int(11) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedAt` datetime(6) DEFAULT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `DelayedUntil` datetime(6) NOT NULL DEFAULT '0001-01-01 00:00:00.000000',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Emails`
--

DROP TABLE IF EXISTS `Emails`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Emails` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `To` longtext DEFAULT NULL,
  `From` longtext DEFAULT NULL,
  `Subject` longtext DEFAULT NULL,
  `Body` longtext DEFAULT NULL,
  `BodyType` longtext DEFAULT NULL,
  `Status` longtext DEFAULT NULL,
  `Error` longtext DEFAULT NULL,
  `Sent` longtext DEFAULT NULL,
  `SentAt` datetime(6) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `DelayedUntil` datetime(6) NOT NULL DEFAULT '0001-01-01 00:00:00.000000',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PlanningSites`
--

DROP TABLE IF EXISTS `PlanningSites`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PlanningSites` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `AreaRulePlanningsId` int(11) NOT NULL,
  `SiteId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `AreaId` int(11) DEFAULT NULL,
  `AreaRuleId` int(11) DEFAULT NULL,
  `Status` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_PlanningSites_AreaRulePlanningsId` (`AreaRulePlanningsId`),
  CONSTRAINT `FK_PlanningSites_AreaRulePlannings_AreaRulePlanningsId` FOREIGN KEY (`AreaRulePlanningsId`) REFERENCES `AreaRulePlannings` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PlanningSitesVersions`
--

DROP TABLE IF EXISTS `PlanningSitesVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PlanningSitesVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `PlanningSiteId` int(11) NOT NULL,
  `PlanningId` int(11) NOT NULL,
  `SiteId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `AreaId` int(11) DEFAULT NULL,
  `AreaRuleId` int(11) DEFAULT NULL,
  `Status` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PluginConfigurationValueVersions`
--

DROP TABLE IF EXISTS `PluginConfigurationValueVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PluginConfigurationValueVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` longtext DEFAULT NULL,
  `Value` longtext DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PluginConfigurationValues`
--

DROP TABLE IF EXISTS `PluginConfigurationValues`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PluginConfigurationValues` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` longtext DEFAULT NULL,
  `Value` longtext DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PluginGroupPermissionVersions`
--

DROP TABLE IF EXISTS `PluginGroupPermissionVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PluginGroupPermissionVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `GroupId` int(11) NOT NULL,
  `PermissionId` int(11) NOT NULL,
  `IsEnabled` tinyint(1) NOT NULL,
  `PluginGroupPermissionId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PluginGroupPermissions`
--

DROP TABLE IF EXISTS `PluginGroupPermissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PluginGroupPermissions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `GroupId` int(11) NOT NULL,
  `PermissionId` int(11) NOT NULL,
  `IsEnabled` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_PluginGroupPermissions_PermissionId` (`PermissionId`),
  CONSTRAINT `FK_PluginGroupPermissions_PluginPermissions_PermissionId` FOREIGN KEY (`PermissionId`) REFERENCES `PluginPermissions` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PluginPermissions`
--

DROP TABLE IF EXISTS `PluginPermissions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PluginPermissions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `PermissionName` longtext DEFAULT NULL,
  `ClaimName` longtext DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PoolAccidentVersions`
--

DROP TABLE IF EXISTS `PoolAccidentVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PoolAccidentVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `PoolAccidentId` int(11) NOT NULL,
  `AreaRuleId` int(11) NOT NULL,
  `Date` datetime(6) NOT NULL,
  `Time` time(6) NOT NULL,
  `SolidFaeces` tinyint(1) NOT NULL,
  `DiarrheaLoose` tinyint(1) NOT NULL,
  `Vomit` tinyint(1) NOT NULL,
  `ContactedPersonId` int(11) NOT NULL,
  `OwnPersonId` int(11) NOT NULL,
  `Comment` longtext DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `FolderId` int(11) NOT NULL DEFAULT 0,
  `PlanningId` int(11) NOT NULL DEFAULT 0,
  `SdkCaseId` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PoolAccidents`
--

DROP TABLE IF EXISTS `PoolAccidents`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PoolAccidents` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `AreaRuleId` int(11) NOT NULL,
  `Date` datetime(6) NOT NULL,
  `Time` time(6) NOT NULL,
  `SolidFaeces` tinyint(1) NOT NULL,
  `DiarrheaLoose` tinyint(1) NOT NULL,
  `Vomit` tinyint(1) NOT NULL,
  `ContactedPersonId` int(11) NOT NULL,
  `OwnPersonId` int(11) NOT NULL,
  `Comment` longtext DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `FolderId` int(11) NOT NULL DEFAULT 0,
  `PlanningId` int(11) NOT NULL DEFAULT 0,
  `SdkCaseId` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PoolHistorySiteVersions`
--

DROP TABLE IF EXISTS `PoolHistorySiteVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PoolHistorySiteVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `AreaRuleId` int(11) NOT NULL,
  `Date` datetime(6) NOT NULL,
  `SiteId` int(11) NOT NULL,
  `SdkCaseId` int(11) NOT NULL,
  `PoolHistorySiteId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PoolHistorySites`
--

DROP TABLE IF EXISTS `PoolHistorySites`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PoolHistorySites` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `AreaRuleId` int(11) NOT NULL,
  `Date` datetime(6) NOT NULL,
  `SiteId` int(11) NOT NULL,
  `SdkCaseId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PoolHourResultVersions`
--

DROP TABLE IF EXISTS `PoolHourResultVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PoolHourResultVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `PoolHourResultId` int(11) NOT NULL,
  `PoolHourId` int(11) NOT NULL,
  `Date` datetime(6) NOT NULL,
  `SdkCaseId` int(11) NOT NULL,
  `PlanningId` int(11) NOT NULL,
  `AreaRuleId` int(11) NOT NULL,
  `PulseRateAtOpening` double DEFAULT NULL,
  `ReadPhValue` double DEFAULT NULL,
  `ReadFreeChlorine` double DEFAULT NULL,
  `ReadTemperature` double DEFAULT NULL,
  `NumberOfGuestsAtClosing` double DEFAULT NULL,
  `Clarity` longtext DEFAULT NULL,
  `MeasuredFreeChlorine` double DEFAULT NULL,
  `MeasuredTotalChlorine` double DEFAULT NULL,
  `MeasuredBoundChlorine` double DEFAULT NULL,
  `MeasuredPh` double DEFAULT NULL,
  `AcknowledgmentOfPulseRateAtOpening` longtext DEFAULT NULL,
  `MeasuredTempDuringTheDay` double DEFAULT NULL,
  `Comment` longtext DEFAULT NULL,
  `DoneByUserId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `FolderId` int(11) NOT NULL DEFAULT 0,
  `DoneByUserName` longtext DEFAULT NULL,
  `DoneAt` datetime(6) NOT NULL DEFAULT '0001-01-01 00:00:00.000000',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PoolHourResults`
--

DROP TABLE IF EXISTS `PoolHourResults`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PoolHourResults` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `PoolHourId` int(11) NOT NULL,
  `Date` datetime(6) NOT NULL,
  `SdkCaseId` int(11) NOT NULL,
  `PlanningId` int(11) NOT NULL,
  `AreaRuleId` int(11) NOT NULL,
  `PulseRateAtOpening` double DEFAULT NULL,
  `ReadPhValue` double DEFAULT NULL,
  `ReadFreeChlorine` double DEFAULT NULL,
  `ReadTemperature` double DEFAULT NULL,
  `NumberOfGuestsAtClosing` double DEFAULT NULL,
  `Clarity` longtext DEFAULT NULL,
  `MeasuredFreeChlorine` double DEFAULT NULL,
  `MeasuredTotalChlorine` double DEFAULT NULL,
  `MeasuredBoundChlorine` double DEFAULT NULL,
  `MeasuredPh` double DEFAULT NULL,
  `AcknowledgmentOfPulseRateAtOpening` longtext DEFAULT NULL,
  `MeasuredTempDuringTheDay` double DEFAULT NULL,
  `Comment` longtext DEFAULT NULL,
  `DoneByUserId` int(11) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `FolderId` int(11) NOT NULL DEFAULT 0,
  `DoneByUserName` longtext DEFAULT NULL,
  `DoneAt` datetime(6) NOT NULL DEFAULT '0001-01-01 00:00:00.000000',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PoolHourVersions`
--

DROP TABLE IF EXISTS `PoolHourVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PoolHourVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `AreaRuleId` int(11) NOT NULL,
  `DayOfWeek` int(11) NOT NULL,
  `Index` int(11) NOT NULL,
  `Name` longtext DEFAULT NULL,
  `ItemsPlanningId` int(11) DEFAULT NULL,
  `PoolHourId` int(11) NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `Version` int(11) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedAt` datetime(6) DEFAULT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PoolHours`
--

DROP TABLE IF EXISTS `PoolHours`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PoolHours` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `AreaRuleId` int(11) NOT NULL,
  `DayOfWeek` int(11) NOT NULL,
  `Index` int(11) NOT NULL,
  `Name` longtext DEFAULT NULL,
  `ItemsPlanningId` int(11) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PropertieVersions`
--

DROP TABLE IF EXISTS `PropertieVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PropertieVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `PropertyId` int(11) NOT NULL,
  `Name` longtext DEFAULT NULL,
  `CHR` longtext DEFAULT NULL,
  `Address` longtext DEFAULT NULL,
  `FolderId` int(11) DEFAULT NULL,
  `ItemPlanningTagId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `CVR` longtext DEFAULT NULL,
  `ComplianceStatus` int(11) NOT NULL DEFAULT 0,
  `ComplianceStatusThirty` int(11) NOT NULL DEFAULT 0,
  `FolderIdForTasks` int(11) DEFAULT NULL,
  `WorkorderEnable` tinyint(1) NOT NULL DEFAULT 0,
  `EntitySelectListAreas` int(11) DEFAULT NULL,
  `EntitySelectListDeviceUsers` int(11) DEFAULT NULL,
  `FolderIdForCompletedTasks` int(11) DEFAULT NULL,
  `FolderIdForNewTasks` int(11) DEFAULT NULL,
  `FolderIdForOngoingTasks` int(11) DEFAULT NULL,
  `EntitySearchListChemicals` int(11) DEFAULT NULL,
  `ChemicalLastUpdatedAt` datetime(6) DEFAULT NULL,
  `EntitySearchListChemicalRegNos` int(11) DEFAULT NULL,
  `IndustryCode` longtext DEFAULT NULL,
  `IsFarm` tinyint(1) NOT NULL DEFAULT 0,
  `EntitySearchListPoolWorkers` int(11) DEFAULT NULL,
  `EntitySelectListChemicalAreas` int(11) DEFAULT NULL,
  `MainMailAddress` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Properties`
--

DROP TABLE IF EXISTS `Properties`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `Properties` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` longtext DEFAULT NULL,
  `CHR` longtext DEFAULT NULL,
  `Address` longtext DEFAULT NULL,
  `FolderId` int(11) DEFAULT NULL,
  `ItemPlanningTagId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `CVR` longtext DEFAULT NULL,
  `ComplianceStatus` int(11) NOT NULL DEFAULT 0,
  `ComplianceStatusThirty` int(11) NOT NULL DEFAULT 0,
  `FolderIdForTasks` int(11) DEFAULT NULL,
  `WorkorderEnable` tinyint(1) NOT NULL DEFAULT 0,
  `EntitySelectListAreas` int(11) DEFAULT NULL,
  `EntitySelectListDeviceUsers` int(11) DEFAULT NULL,
  `FolderIdForCompletedTasks` int(11) DEFAULT NULL,
  `FolderIdForNewTasks` int(11) DEFAULT NULL,
  `FolderIdForOngoingTasks` int(11) DEFAULT NULL,
  `EntitySearchListChemicals` int(11) DEFAULT NULL,
  `ChemicalLastUpdatedAt` datetime(6) DEFAULT NULL,
  `EntitySearchListChemicalRegNos` int(11) DEFAULT NULL,
  `IndustryCode` longtext DEFAULT NULL,
  `IsFarm` tinyint(1) NOT NULL DEFAULT 0,
  `EntitySearchListPoolWorkers` int(11) DEFAULT NULL,
  `EntitySelectListChemicalAreas` int(11) DEFAULT NULL,
  `MainMailAddress` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PropertySelectedLanguageVersions`
--

DROP TABLE IF EXISTS `PropertySelectedLanguageVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PropertySelectedLanguageVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `PropertySelectedLanguageId` int(11) NOT NULL,
  `PropertyId` int(11) NOT NULL,
  `LanguageId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=16 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PropertySelectedLanguages`
--

DROP TABLE IF EXISTS `PropertySelectedLanguages`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PropertySelectedLanguages` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `PropertyId` int(11) NOT NULL,
  `LanguageId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_PropertySelectedLanguages_PropertyId` (`PropertyId`),
  CONSTRAINT `FK_PropertySelectedLanguages_Properties_PropertyId` FOREIGN KEY (`PropertyId`) REFERENCES `Properties` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PropertyWorkerVersions`
--

DROP TABLE IF EXISTS `PropertyWorkerVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PropertyWorkerVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `PropertyId` int(11) NOT NULL,
  `WorkerId` int(11) NOT NULL,
  `PropertyWorkerId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `EntityItemId` int(11) DEFAULT NULL,
  `TaskManagementEnabled` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PropertyWorkers`
--

DROP TABLE IF EXISTS `PropertyWorkers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `PropertyWorkers` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `PropertyId` int(11) NOT NULL,
  `WorkerId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `EntityItemId` int(11) DEFAULT NULL,
  `TaskManagementEnabled` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_PropertyWorkers_PropertyId` (`PropertyId`),
  CONSTRAINT `FK_PropertyWorkers_Properties_PropertyId` FOREIGN KEY (`PropertyId`) REFERENCES `Properties` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ProperyAreaFolderVersions`
--

DROP TABLE IF EXISTS `ProperyAreaFolderVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ProperyAreaFolderVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ProperyAreaFolderId` int(11) NOT NULL,
  `ProperyAreaAsignmentId` int(11) NOT NULL,
  `FolderId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=31 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ProperyAreaFolders`
--

DROP TABLE IF EXISTS `ProperyAreaFolders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ProperyAreaFolders` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ProperyAreaAsignmentId` int(11) NOT NULL,
  `FolderId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ProperyAreaFolders_ProperyAreaAsignmentId` (`ProperyAreaAsignmentId`),
  CONSTRAINT `FK_ProperyAreaFolders_AreaProperties_ProperyAreaAsignmentId` FOREIGN KEY (`ProperyAreaAsignmentId`) REFERENCES `AreaProperties` (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=31 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `WorkorderCaseImageVersions`
--

DROP TABLE IF EXISTS `WorkorderCaseImageVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `WorkorderCaseImageVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `WorkorderCaseImageId` int(11) NOT NULL,
  `WorkorderCaseId` int(11) NOT NULL,
  `UploadedDataId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `WorkorderCaseImages`
--

DROP TABLE IF EXISTS `WorkorderCaseImages`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `WorkorderCaseImages` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `WorkorderCaseId` int(11) NOT NULL,
  `UploadedDataId` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `WorkorderCaseVersions`
--

DROP TABLE IF EXISTS `WorkorderCaseVersions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `WorkorderCaseVersions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `WorkorderCaseId` int(11) NOT NULL,
  `PropertyWorkerId` int(11) NOT NULL,
  `CaseId` int(11) NOT NULL,
  `CaseStatusesEnum` int(11) NOT NULL,
  `ParentWorkorderCaseId` int(11) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `EntityItemIdForArea` int(11) DEFAULT NULL,
  `SelectedAreaName` longtext DEFAULT NULL,
  `CreatedByName` longtext DEFAULT NULL,
  `CreatedByText` longtext DEFAULT NULL,
  `Description` longtext DEFAULT NULL,
  `CaseInitiated` datetime(6) NOT NULL DEFAULT '0001-01-01 00:00:00.000000',
  `LastAssignedToName` longtext DEFAULT NULL,
  `LastUpdatedByName` longtext DEFAULT NULL,
  `LeadingCase` tinyint(1) NOT NULL DEFAULT 0,
  `Priority` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `WorkorderCases`
--

DROP TABLE IF EXISTS `WorkorderCases`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `WorkorderCases` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `PropertyWorkerId` int(11) NOT NULL,
  `CaseId` int(11) NOT NULL,
  `CaseStatusesEnum` int(11) NOT NULL,
  `ParentWorkorderCaseId` int(11) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `WorkflowState` varchar(255) DEFAULT NULL,
  `CreatedByUserId` int(11) NOT NULL,
  `UpdatedByUserId` int(11) NOT NULL,
  `Version` int(11) NOT NULL,
  `EntityItemIdForArea` int(11) DEFAULT NULL,
  `SelectedAreaName` longtext DEFAULT NULL,
  `CreatedByName` longtext DEFAULT NULL,
  `CreatedByText` longtext DEFAULT NULL,
  `Description` longtext DEFAULT NULL,
  `CaseInitiated` datetime(6) NOT NULL DEFAULT '0001-01-01 00:00:00.000000',
  `LastAssignedToName` longtext DEFAULT NULL,
  `LastUpdatedByName` longtext DEFAULT NULL,
  `LeadingCase` tinyint(1) NOT NULL DEFAULT 0,
  `Priority` longtext DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_WorkorderCases_ParentWorkorderCaseId` (`ParentWorkorderCaseId`),
  KEY `IX_WorkorderCases_PropertyWorkerId` (`PropertyWorkerId`),
  CONSTRAINT `FK_WorkorderCases_PropertyWorkers_PropertyWorkerId` FOREIGN KEY (`PropertyWorkerId`) REFERENCES `PropertyWorkers` (`Id`),
  CONSTRAINT `FK_WorkorderCases_WorkorderCases_ParentWorkorderCaseId` FOREIGN KEY (`ParentWorkorderCaseId`) REFERENCES `WorkorderCases` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `__EFMigrationsHistory`
--

DROP TABLE IF EXISTS `__EFMigrationsHistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `__EFMigrationsHistory` (
  `MigrationId` varchar(150) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__EFMigrationsHistory`
--

LOCK TABLES `__EFMigrationsHistory` WRITE;
/*!40000 ALTER TABLE `__EFMigrationsHistory` DISABLE KEYS */;
INSERT INTO `__EFMigrationsHistory` VALUES ('20211025142151_InitialCreate','7.0.1'),('20211110114709_AddingCVRToProperties','7.0.1'),('20220101103446_AddingPropertyIdToAreaRulePlanning','7.0.1'),('20220101111025_AddingAreaIdToAreaRulePlanning','7.0.1'),('20220103152353_AddingComplianceStatusToProperty','7.0.1'),('20220105092557_CreateComplianceClass','7.0.1'),('20220105135743_ChangingAreaIdToInt','7.0.1'),('20220105140315_AddingMoreAttributesToCompliance','7.0.1'),('20220106165734_AddingComplianceStatus30ToProperties','7.0.1'),('20220127172400_AddComplianceEnabledProp','7.0.1'),('20220208172326_AddingPlanningCaseSiteIdAndCheckListSiteId','7.0.1'),('20220210171743_AddWorkorderFlow','7.0.1'),('20220216140313_AddEntitySelectListsForProperty','7.0.1'),('20220217112234_AddingAdditionalFolderIds','7.0.1'),('20220217133829_AddingEntityItemIdToPropertyWorker','7.0.1'),('20220217165554_AddingAttributesToWorkorderCase','7.0.1'),('20220217174831_AddingCreatedByNameToWorkorderCase','7.0.1'),('20220217182010_AddingCreatedByTextToWorkorderCase','7.0.1'),('20220217183352_AddingDescriptionToWorkorderCase','7.0.1'),('20220217184356_AddingCaseInitiatedToWorkorderCase','7.0.1'),('20220217193352_CreateWorkorderCaseImage','7.0.1'),('20220321151057_AddFieldsPlaceholderAndInfoForAreas','7.0.1'),('20220405183346_AddingNewItemName','7.0.1'),('20220417055742_AddingAssignmentAttributesToWorkorderCases','7.0.1'),('20220425054126_AddRepeatTypeToAreaRule','7.0.1'),('20220425151849_AddingMoreAttributesToAreaRule','7.0.1'),('20220426190544_AddingLeadingCaseToWorkorderCase','7.0.1'),('20220603160201_AddingEntitySearchListChemicalsToProperty','7.0.1'),('20220606072856_AddingEntitySearchListChemicalRegNosToProperty','7.0.1'),('20220607114609_CreatingChemicalProductProperty','7.0.1'),('20220609162637_AddingIndustryCodeIsFarmToProperty','7.0.1'),('20220610080134_FixingIndustryCodeTobeString','7.0.1'),('20220612043907_AddingIsFarmToArea','7.0.1'),('20220612164010_AddingPoolHours','7.0.1'),('20220613055856_AddingMorePoolHourTables','7.0.1'),('20220613132009_AllowingItemPlanningIdToBeNull','7.0.1'),('20220614143506_AddingMoreIdsToPlanningSites','7.0.1'),('20220616160212_AddingEntitySearchListPoolWorkers','7.0.1'),('20220621070937_AddingFolderIdToPoolRegistrations','7.0.1'),('20220623095747_ChangingAttributeTypes','7.0.1'),('20220623173220_CreatingPoolHistorySite','7.0.1'),('20220623184950_AllowingValuesToBeNull','7.0.1'),('20220630080523_AddingDoneAtToPoolHourResult','7.0.1'),('20220630161000_AddingSdkCaseIdToChemicalProductProperty','7.0.1'),('20220703144301_AddingChemicalProductPropertySite','7.0.1'),('20220705160545_AddingEntitySelectListChemicalAreasToProperty','7.0.1'),('20220724155019_AddingLocationsToChemicalProductProperty','7.0.1'),('20220806155246_AddingLanguageIdAndSdkSiteIdToChemicalProductProperty','7.0.1'),('20220810024655_AddingMainMailAddressToProperty','7.0.1'),('20220824154458_AddingExpireDateToChemicalProductProperty','7.0.1'),('20220828101920_AddingSecondaryeFormIdToAreaRule','7.0.1'),('20220905153327_AddingLastFolderNameToChemicalProductProperty','7.0.1'),('20221019143021_AddingEmail','7.0.1'),('20221019143124_AddingEmailDelayed','7.0.1'),('20221023112526_AddingEmailAttachment','7.0.1'),('20221108183349_AddingPriorityToWorkOrderCase','7.0.1'),('20221120065821_AddingTaskManagementEnabledToPropertyWorker','7.0.1'),('20221215120936_AddingStatusToPlanningSite','7.0.1');
/*!40000 ALTER TABLE `__EFMigrationsHistory` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2022-12-18 20:24:51
