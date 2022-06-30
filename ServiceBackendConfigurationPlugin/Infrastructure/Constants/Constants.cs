using System.Collections.Generic;

namespace ServiceBackendConfigurationPlugin.Infrastructure.Constants;

public static class Constants
{
    public static Dictionary<int, string> ActiveSubstanceActionMechanism = new()
    {
        { 1, "1" },
        { 3, "3" },
        { 4, "4" },
        { 5, "5" },
        { 6, "6" },
        { 7, "7" },
        { 9, "9" },
        { 11, "11" },
        { 12, "12" },
        { 13, "13" },
        { 14, "14" },
        { 15, "15" },
        { 16, "16" },
        { 17, "17" },
        { 20, "20" },
        { 21, "21" },
        { 22, "22" },
        { 23, "23" },
        { 27, "27" },
        { 28, "28" },
        { 29, "29" },
        { 31, "31" },
        { 32, "32" },
        { 40, "40" },
        { 50, "50" },
        { 51, "1A" },
        { 52, "1B" },
        { 53, "3A" },
        { 54, "4A" },
        { 55, "7C" },
        { 56, "9B" },
        { 57, "10A" },
        { 58, "20D" },
        { 59, "21A" },
        { 60, "22A" },
        { 61, "24A" },
        { 62, "A / 1" },
        { 63, "B / 2" },
        { 64, "C1 / 5" },
        { 65, "C2 / 5" },
        { 66, "C3 / 6" },
        { 67, "D / 22" },
        { 68, "E / 14" },
        { 69, "F1 / 12" },
        { 70, "F2 / 27" },
        { 71, "F3 / 34" },
        { 72, "F4 / 13" },
        { 73, "G / 9" },
        { 74, "I / 18" },
        { 75, "Ikke klassificeret" },
        { 76, "K1 / 3" },
        { 77, "M02" },
        { 78, "M03" },
        { 79, "M04" },
        { 80, "M09" },
        { 81, "K3 / 15" },
        { 82, "O / 4" },
        { 83, "P04" },
        { 84, "P07" },
        { 85, "U12" },
        { 86, "UN" },
        { 87, "UNE" },
        { 88, "UNM" },
        { 89, "Z / 0" },
        { 100, "Ikke relevant" },
        { 101, "Øvrige" }
    };

    public static Dictionary<int, string> ActiveSubstanceType = new()
    {
        { 1, "Kemisk" },
        { 2, "Mikrobiologisk" }
    };

    public static Dictionary<int, string> AuthorizationTypeBiocide = new()
    {
        { 1, "Aktivstof med produkt" },
        { 2, "Me-too" },
        { 3, "Midlertidig godkendelse" },
        { 4, "Identisk produkt" },
        { 5, "Parallel import" },
        { 11, "Dispensation" },
        { 12, "Dispensation tidligere vurderet" },
        { 13, "National afgørelse (NA-APP)" },
        { 14, "Gensidig anerkendelse efterfølgende (NA-MRS)" },
        { 15, "Gensidig anerkendelse parallel (NA-MRP)" },
        { 16, "EU-godkendelse (UA-APP)" },
        { 17, "Forenklet procedure (SA-APP)" },
        { 18, "Parallelhandel (PP-APP)" },
        { 19, "Sammenfaldende produkter under godkendelse (NA-BBP)" },
        { 20, "Sammenfaldende produkter allerede godkendt (NA-BBS)" },
        { 21, "Midlertidig godkendelse (NA-PROV)" },
        { 22, "Forsøgsmæssig afprøvning (NA-NOT)" },
        { 23, "Dispensation (NA-DISP)" },
        { 24, "Dispensation - tidligere vurderet (NA-DISP)" },
        { 25, "Notificering af produkt efter simplificeret procedure (SN-NOT)" },
        { 36, "Dispensation – tidligere vurderet produkt godkendt i EU (NA-DISP)" },
        { 37, "Notificering af produkt efter Unionsgodkendelse (UA-NOT)" },
        { 39, "Notificering af nyt produktfamiliemedlem i en eksisterende produktfamiliegodkendelse (FA-NOT)" }
    };

    public static Dictionary<int, string> AuthorizationTypePesticide = new()
    {
        { 1, "Almindelig" },
        { 2, "Gensidig anerkendelse" },
        { 3, "Parallelimport" },
        { 4, "Kopi" },
        { 5, "Dispensation" },
        { 6, "Forsøgsmæssig afprøvning" }
    };

    public static Dictionary<int, string> BeeSymbol = new()
    {
        { 1, "Farlig" },
        { 2, "Meget farlig" }
    };

    public static Dictionary<int, string> CategoryCode = new()
    {
        { 1, "1" },
        { 2, "2" },
        { 3, "3" },
        { 4, "4" },
        { 5, "-" },
        { 6, "1A" },
        { 7, "1B" },
        { 8, "1C" }
    };

    public static Dictionary<int, string> FormulationSubType = new()
    {
        { 1, "Kornet lokkemiddel" },
        { 2, "Aerosol" },
        { 3, "Opløsning til brug ufortyndet" },
        { 4, "Lokkemiddel i blokform" },
        { 5, "Briket" },
        { 6, "Koncentrat til lokkemad" },
        { 7, "Kapselgranulat" },
        { 8, "Kapselsuspension" },
        { 9, "Dispergerbart koncentrat" },
        { 10, "Pudder" },
        { 11, "Tørbejse" },
        { 12, "Emulgerbart koncentrat (emulsionskoncentrat)" },
        { 13, "Opløsning til udsprøjtning som elektrisk ladende dråber" },
        { 14, "Vand i olie emulsion (omvendt emulsion)" },
        { 15, "Emulsionsbejdse" },
        { 16, "Olie i vand emulsion" },
        { 17, "Rygedåse" },
        { 18, "Fingranulat" },
        { 19, "Rygelys" },
        { 20, "Røgpatron" },
        { 21, "Rygestav" },
        { 22, "Suspensionspræpaparat til bejdsning" },
        { 23, "Rygetablet" },
        { 24, "Rygemiddel" },
        { 25, "Røgpille" },
        { 26, "Gas (under tryk)" },
        { 27, "Granuleret lokkemiddel" },
        { 28, "Gasudviklende produkt" },
        { 29, "Makrogranulat" },
        { 30, "Finpudder" },
        { 31, "Granulat" },
        { 32, "Pasta på olie- eller fedtbasis" },
        { 33, "Tågekoncentrat (skal opvarmes)" },
        { 34, "Tågekoncentrat til koldt brug" },
        { 35, "Lak" },
        { 36, "Opløsning til bejdsning" },
        { 37, "Mikrogranulat" },
        { 38, "Olieblandbart flydbart koncentrat" },
        { 39, "Olieblandbar opløsning" },
        { 40, "Oliedispergerbart pulver" },
        { 41, "Pasta på vandbasis" },
        { 42, "Lokkemiddel på plade" },
        { 43, "Pastakoncentrat" },
        { 44, "Imprægneret stav" },
        { 45, "Pilleret frø" },
        { 46, "Brugsfærdigt lokkemiddel" },
        { 47, "Lokkemiddel i småbidder" },
        { 48, "Suspensionskoncentrat" },
        { 49, "Suspoemulsionskoncentrat" },
        { 50, "Vandopløseligt granulat" },
        { 51, "Vandopløseligt koncentrat" },
        { 52, "Filmdannende olie" },
        { 53, "Vandopløseligt pulver" },
        { 54, "Vandopløseligt pulver til bejdsning" },
        { 55, "ULV-suspensionskoncentrat" },
        { 56, "Tablet" },
        { 57, "Strøpulver" },
        { 58, "ULV-opløsning" },
        { 59, "Produkt der afgiver aktivstof i dampform" },
        { 60, "Vanddispergerbart granulat" },
        { 61, "Vanddispergerbart pulver" },
        { 62, "Vanddispergerbart pulver til bejdsning i opslemning" },
        { 63, "Andre produkter" }
    };

    public static Dictionary<int, string> FormulationType = new()
    {
        { 1, "Væske, herunder aerosolspray og pasta" },
        { 2, "Granulat" },
        { 3, "Pellet" },
        { 4, "Vandopløselig tablet" },
        { 5, "Pind" },
        { 6, "Vandopløselig pose" },
        { 7, "Gaspatron" },
        { 8, "Pulver" },
        { 9, "Ukendt" },
        { 10, "Andet" }
    };

    public static Dictionary<int, string> GHSHazardPictogram = new()
    {
        { 1, "GHS01ExplodingBomb" },
        { 2, "GHS02Flame" },
        { 3, "GHS03FlameOverCircle" },
        { 4, "GHS04GasCylinder" },
        { 5, "GHS05Corrosion" },
        { 6, "GHS06SkullAndCrossbones" },
        { 7, "GHS07ExclamationMark" },
        { 8, "GHS08HealthHazard" },
        { 9, "GHS09AquaticHazard" }
    };

    public static Dictionary<int, string> HazardClass = new()
    {
        {1	,"Brandfarlige væsker (Flam. Liq.)"},
        {2	,"Aerosol (Aerosol)"},
        {3	,"Akut toksicitet (Acute Tox.)"},
        {4	,"Hudætsning/-irritation (Skin Corr.)"},
        {5	,"Hudætsning/-irritation (Skin Irrit.)"},
        {6	,"Alvorlig øjenskade/øjenirritation (Eye Dam.)"},
        {7	,"Alvorlig øjenskade/øjenirritation (Eye irrit.)"},
        {8	,"Sensibilisering ved indånding eller hudsensibilisering (Resp. Sens.)"},
        {9	,"Sensibilisering ved indånding eller hudsensibilisering (Skin sens.)"},
        {10	,"Kimcellemutagenicitet (Muta.)"},
        {11	,"Carcinogenicitet (Carc.)"},
        {12	,"Reproduktionstoksicitet (Repr.)"},
        {13	,"Reproduktionstoksicitet (Lact.)"},
        {14	,"Specifik målorgantoksicitet (STOT) – enkelt eksponering (STOT SE)"},
        {15	,"Specifik målorgantoksicitet (STOT) – gentagen eksponering (STOT RE)"},
        {16	,"Aspirationstoksicitet (Asp. Tox.)"},
        {17	,"Vandmiljø (Aquatic Acute)"},
        {18	,"Vandmiljø (Aquatic Chronic)"},
        {19	,"Øvrige fareklasser"}
    };

    public static Dictionary<int, string> HazardStatement = new()
    {
        { 1, "Ustabilt eksplosiv (H200)" },
        { 2, "Eksplosiv, masseeksplosionsfare (H201)" },
        { 3, "Eksplosiv, alvorlig fare for udslyngning af fragmenter (H202)" },
        { 4, "Eksplosiv, fare for brand, eksplosion eller udslyngning af fragmenter (H203)" },
        { 5, "Fare for brand eller udslyngning af fragmenter (H204)" },
        { 6, "Fare for masseeksplosion ved brand (H205)" },
        { 7, "Yderst brandfarlig gas (H220)" },
        { 8, "Brandfarlig gas (H221)" },
        { 9, "Yderst brandfarlig aerosol (H222)" },
        { 10, "Brandfarlig aerosol (H223)" },
        { 11, "Yderst brandfarlig væske og damp (H224)" },
        { 12, "Meget brandfarlig væske og damp (H225)" },
        { 13, "Brandfarlig væske og damp (H226)" },
        { 14, "Brandfarligt fast stof (H228)" },
        { 15, "Beholder under tryk – kan sprænge ved opvarmning (H229)" },
        { 16, "Eksplosionsfare ved opvarmning (H240)" },
        { 17, "Brand- eller eksplosionsfare ved opvarmning (H241)" },
        { 18, "Brandfare ved opvarmning  (H242)" },
        { 19, "Selvantænder ved kontakt med luft (H250)" },
        { 20, "Selvopvarmende, kan selvantænde (H251)" },
        { 21, "Selvopvarmende i store mængder, kan selvantænde (H252)" },
        { 22, "Ved kontakt med vand udvikles brandfarlige gasser, som kan selvantænde (H260)" },
        { 23, "Ved kontakt med vand udvikles brandfarlige gasser (H261)" },
        { 24, "Kan forårsage eller forstærke brand, brandnærende (H270)" },
        { 25, "Kan forårsage brand eller eksplosion, stærkt brandnærende (H271)" },
        { 26, "Kan forstærke brand, brandnærende (H272)" },
        { 27, "Indeholder gas under tryk, kan eksplodere ved opvarmning (H280)" },
        { 28, "Indeholder nedkølet gas, kan forårsage kuldeskader (H281)" },
        { 29, "Kan ætse metaller (H290)" },
        { 30, "Livsfarlig ved indtagelse (H300)" },
        { 31, "Giftig ved indtagelse (H301)" },
        { 32, "Farlig ved indtagelse (H302)" },
        { 33, "Kan være livsfarligt, hvis det indtages og kommer i luftvejene (H304)" },
        { 34, "Livsfarlig ved hudkontakt (H310)" },
        { 35, "Giftig ved hudkontakt (H311)" },
        { 36, "Farlig ved hudkontakt (H312)" },
        { 37, "Forårsager svære forbrændinger af huden og øjenskader (H314)" },
        { 38, "Forårsager hudirritation (H315)" },
        { 39, "Kan forårsage allergisk hudreaktion (H317)" },
        { 40, "Forårsager alvorlig øjenskade (H318)" },
        { 41, "Forårsager alvorlig øjenirritation (H319)" },
        { 42, "Livsfarlig ved indånding (H330)" },
        { 43, "Giftig ved indånding (H331)" },
        { 44, "Farlig ved indånding (H332)" },
        { 45, "Kan forårsage allergi- eller astmasymptomer eller åndedrætsbesvær ved indånding (H334)" },
        { 46, "Kan forårsage irritation af luftvejene (H335)" },
        { 47, "Kan forårsage sløvhed eller svimmelhed (H336)" },
        { 48, "Kan forårsage genetiske defekter (H340)" },
        { 49, "Mistænkt for at forårsage genetiske defekter (H341)" },
        { 50, "Kan fremkalde kræft (H350)" },
        { 51, "Mistænkt for at fremkalde kræft (H351)" },
        { 52, "Kan skade forplantningsevnen (H360F)" },
        { 53, "Kan skade  det ufødte barn(H360D)" },
        { 54, "Kan skade forplantningsevnen eller det ufødte barn (H360FD)" },
        { 55, "Kan skade forplantningsevnen eller mistænkt for at skade det ufødte barn (H360Fd)" },
        { 56, "Kan skade det ufødte barn. Mistænkt for at skade forplantningsevnen (H360Df)" },
        { 57, "Mistænkt for at skade forplantningsevnen (H361f)" },
        { 58, "Mistænkt for at skade det ufødte barn (H361d)" },
        { 59, "Mistænkt for at skade forplantningsevnen eller det ufødte barn (H361fd)" },
        { 60, "Kan skade børn, der ammes (H362)" },
        { 61, "Forårsager organskader (H370)" },
        { 62, "Kan forårsage organskader (H371)" },
        { 63, "Forårsager organskader ved længerevarende eller gentagen eksponering (H372)" },
        { 64, "Kan forårsage organskader ved længerevarende eller gentagen eksponering (H373)" },
        { 65, "Meget giftig for vandlevende organismer (H400)" },
        { 66, "Meget giftig med langvarige virkninger for vandlevende organismer (H410)" },
        { 67, "Giftig for vandlevende organismer, med langvarige virkninger (H411)" },
        { 68, "Skadelig for vandlevende organismer, med langvarige virkninger (H412)" },
        { 69, "Kan forårsage langvarige skadelige virkninger for vandlevende organismer (H413)" },
        { 70, "Eksplosiv i tør tilstand (EUH 001)" },
        { 71, "Eksplosiv ved og uden kontakt med luft (EUH 006)" },
        { 72, "Reagerer voldsomt med vand (EUH 014)" },
        { 73, "Ved brug kan brandbarlige dampe/eksplosive damp-luftblandinger dannes (EUH 018)" },
        { 74, "Kan danne eksplosive peroxider (EUH 019)" },
        { 75, "Eksplosionsfarlig ved opvarmning under indeslutning (EUH 044)" },
        { 76, "Udvikler giftig gas ved kontakt med vand (EUH 029)" },
        { 77, "Udvikler giftig gas ved kontakt med syre (EUH 031)" },
        { 78, "Udvikler meget giftig gas ved kontakt med syre (EUH 032)" },
        { 79, "Gentagen kontakt kan give tør eller revnet hud (EUH 066)" },
        { 80, "Giftig ved kontakt med øjnene (EUH 070)" },
        { 81, "Ætsende for luftvejene (EUH 071)" },
        { 82, "Farlig for ozonlaget (EUH 059)" },
        { 83, "Kan udløse allergisk reaktion (EUH 208)" },
        { 84, "Brugsanvisningen skal følges for ikke at bringe menneskers sundhed og miljøet i fare (EUH 401)" }
    };

    public static Dictionary<int, string> IndicationDangerEnvironment = new()
    {
        { 1, "Miljøfarlig (N)" }
    };

    public static Dictionary<int, string> IndicationDangerFlammable = new()
    {
        { 1, "Eksplosiv (E)" },
        { 2, "Brandnærende (O)" },
        { 3, "Brandfarlig (B)" },
        { 4, "Meget brandfarlig (F)" },
        { 5, "Yderst brandfarlig (Fx)" }
    };

    public static Dictionary<int, string> IndicationDangerToxicity = new()
    {
        { 1, "Giftig (T)" },
        { 2, "Meget giftig (Tx)" },
        { 3, "Ætsende (C)" },
        { 4, "Sundhedsskadelig (Xn)" },
        { 5, "Lokalirriterende (Xi)" },
    };

    public static Dictionary<int, string> PestControlType = new()
    {
        { 1, "Biocid" },
        { 2, "Pesticid" }
    };

    public static Dictionary<int, string> PossibleUseBiocide = new()
    {
        { 1, "Udendørs brug" },
        { 2, "Indendørs brug" },
        { 3, "Andet" }
    };

    public static Dictionary<int, string> PossibleUsePesticide = new()
    {
        { 1, "Anvendes på friland" },
        { 2, "Kun til væksthuse" },
        { 3, "Kun til høstede afgrøder i kornlagre o.l." },
        { 4, "Brugsfærdig opløsning (klar-til-brug)" },
        { 5, "Bejdse til korn / frø" },
        { 6, "Bejdse til roer / kartofler / blomsterløg / knolde" },
        { 7, "Bejdse kun til industriel anvendelse" },
        { 8, "Kun til eksport" },
        { 9, "Anvendes på friland og i væksthuse" }
    };

    public static Dictionary<int, string> ProductGroupPesticide = new()
    {
        { 1, "Ukrudtsmidler (inkl. nedvisningsmidler)" },
        { 2, "Vækstreguleringsmidler (inkl. spiringshæmmende midler)" },
        { 3, "Algemidler og desinfektionsmidler til plantebeskyttelse" },
        { 4, "Svampemidler" },
        { 5, "Jorddesinfektionsmidler" },
        { 6, "Nematicider" },
        { 7, "Insektmidler (inkl. kornskadedyr)" },
        { 8, "Acaricider" },
        { 9, "Rodenticider - muldvarpe og mosegrise" },
        { 10, "Afskrækningsmidler (repellanter)" },
        { 11, "Sneglemidler" },
        { 12, "Tiltrækningsmidler" },
        { 13, "Baktericider" },
        { 14, "Elicitorer" },
        { 15, "Anden behandling" },
        { 16, "Planteaktivatorer" },
        { 17, "Virus inokulering" }
    };

    public static Dictionary<int, string> ProductStatusType = new()
    {
        { 1, "Ansøgning om nyt produkt modtaget" },
        { 2, "Ansøgning om nyt produkt trukket" },
        { 3, "Ansøgning om nyt produkt returneret" },
        { 4, "Ansøgning om nyt produkt afslået" },
        { 5, "Produkt godkendt" },
        { 6, "Produkt afmeldt" },
        { 7, "Produkt udløbet" },
        { 8, "Produkt afslået" },
        { 9, "Ansøgning om nyt produkt annulleret" }
    };

    public static Dictionary<int, string> RiskPhrase = new()
    {
        { 1, "R1 – Eksplosiv i tør tilstand" },
        { 2, "R2 – Eksplosionsfarlig ved stød, gnidning, ild eller andre antændelseskilder" },
        { 3, "R3 – Meget eksplosionsfarlig ved stød, gnidning, ild eller andre antændelseskilder" },
        { 4, "R4 – Danner meget følsomme eksplosive metalforbindelser" },
        { 5, "R5 – Eksplosionsfarlig ved opvarmning" },
        { 6, "R6 – Eksplosiv ved og uden kontakt med luft" },
        { 7, "R7 – Kan forårsage brand" },
        { 8, "R8 – Brandfarlig ved kontakt med brandbare stoffer" },
        { 9, "R9 – Eksplosionsfarlig ved blanding med brandbare stoffer" },
        { 10, "R10 – Brandfarlig" },
        { 11, "R11 – Meget brandfarlig" },
        { 12, "R12 – Yderst brandfarlig" },
        { 13, "R14 – Reagerer voldsomt med vand" },
        { 14, "R15 – Reagerer med vand under dannelse af yderst brandfarlige gasser" },
        { 15, "R16 – Eksplosionsfarlig ved blanding med oxiderende stoffer" },
        { 16, "R17 – Selvantændelig i luft" },
        { 17, "R18 – Ved brug kan brandbare dampe/eksplosive damp- luftblandinger dannes" },
        { 18, "R19 – Kan danne eksplosive peroxider" },
        { 19, "R20 – Farlig ved indånding" },
        { 20, "R21 – Farlig ved hudkontakt" },
        { 21, "R22 – Farlig ved indtagelse" },
        { 22, "R23 – Giftig ved indånding" },
        { 23, "R24 – Giftig ved hudkontakt" },
        { 24, "R25 – Giftig ved indtagelse" },
        { 25, "R26 – Meget giftig ved indånding" },
        { 26, "R27 – Meget giftig ved hudkontakt" },
        { 27, "R28 – Meget giftig ved indtagelse" },
        { 28, "R29 – Udvikler giftig gas ved kontakt med vand" },
        { 29, "R30 – Kan blive meget brandfarlig under brug" },
        { 30, "R31 – Udvikler giftig gas ved kontakt med syre" },
        { 31, "R32 – Udvikler meget giftig gas ved kontakt med syre" },
        { 32, "R33 – Kan ophobes i kroppen efter gentagen brug" },
        { 33, "R34 – Ætsningsfare" },
        { 34, "R35 – Alvorlig ætsningsfare" },
        { 35, "R36 – Irriterer øjnene" },
        { 36, "R37 – Irriterer åndedrætsorganerne" },
        { 37, "R38 – Irriterer huden" },
        { 38, "R39 – Fare for varig alvorlig skade på helbred" },
        { 39, "R40 – Mulighed for kræftfremkaldende effekt" },
        { 40, "R41 – Risiko for alvorlig øjenskade" },
        { 41, "R42 – Kan give overfølsomhed ved indånding" },
        { 42, "R43 – Kan give overfølsomhed ved kontakt med huden" },
        { 43, "R44 – Eksplosionsfarlig ved opvarmning under indeslutning" },
        { 44, "R45 – Kan fremkalde kræft" },
        { 45, "R46 – Kan forårsage arvelige genetiske skader" },
        { 46, "R48 – Alvorlig sundhedsfare ved længere tids påvirkning" },
        { 47, "R49 – Kan fremkalde kræft ved indånding" },
        { 48, "R50 – Meget giftig for organismer, der lever i vand" },
        { 49, "R51 – Giftig for organismer, der lever i vand" },
        { 50, "R52 – Skadelig for organismer, der lever i vand" },
        { 51, "R53 – Kan forårsage uønskede langtidsvirkninger i vandmiljøet" },
        { 52, "R54 – Giftig for planter" },
        { 53, "R55 – Giftig for dyr" },
        { 54, "R56 – Giftig for organismer i jordbunden" },
        { 55, "R57 – Giftig for bier" },
        { 56, "R58 – Kan forårsage uønskede langtidsvirkninger i miljøet" },
        { 57, "R59 – Farlig for ozonlaget" },
        { 58, "R60 – Kan skade forplantningsevnen" },
        { 59, "R61 – Kan skade barnet under graviditeten" },
        { 60, "R62 – Mulighed for skade på forplantningsevnen" },
        { 61, "R63 – Mulighed for skade på barnet under graviditeten" },
        { 62, "R64 – Kan skade børn i ammeperioden" },
        { 63, "R65 – Farlig – kan give lungeskade ved indtagelse" },
        { 64, "R66 – Gentagen udsættelse kan give tør eller revnet hud" },
        { 65, "R67 – Dampe kan give sløvhed og svimmelhed" },
        { 66, "R68 – Mulighed for varig skade på helbred" }
    };

    public static Dictionary<int, string> SignalWord = new()
    {
        { 1, "Fare" },
        { 2, "Advarsel" },
        { 3, "Forsigtig" },
    };

    public static Dictionary<int, string> SpecialUseType = new()
    {
        { 1, "Kan anvendes på rekreative græsarealer" },
        { 2, "Kan anvendes på golfbaner" }
    };

    public static Dictionary<int, string> Unit = new()
    {
        { 1, "g/kg" },
        { 2, "g/l" },
        { 3, "cfu/kg" },
        { 4, "cfu/l" },
        { 5, "IU/kg" },
        { 6, "IU/l" },
        { 7, "Granulater/kg" },
        { 8, "Granulater/l" },
        { 9, "% v/v" },
        { 10, "Virus/ml" },
        { 11, "Virus/l" },
        { 12, "Virus/g" },
        { 13, "Virus/kg" }
    };

    public static Dictionary<int, string> UserType = new()
    {
        { 1, "Professionel" },
        { 2, "Ikke-professionel" }
    };

    public static Dictionary<int, string> UserTypeBiocide = new()
    {
        { 1, "Professionel" },
        { 2, "Privat" },
        { 3, "Industriel" },
        { 4, "Trænet professionel" }
    };
}

