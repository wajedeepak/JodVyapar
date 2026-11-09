/*
    JodVyapar - 02_SeedData.sql
    Roles, Indian states/UTs, major cities and the starter category tree.

    - Run after 01_CreateTables.sql, against the JodVyapar database.
    - Idempotent: rows are matched on fixed GUIDs and inserted only when missing (existing rows get
      their names/flags refreshed), so it is safe to re-run.
    - The GUIDs are deterministic: MD5("jodvyapar:<key>") with keys such as "role:Buyer",
      "state:MH", "city:MH:Mumbai" and "category:<slug>". Never change an existing Id; to add
      rows later, put them in a new numbered script with new fixed GUIDs.
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON; -- required for the filtered unique indexes (sqlcmd defaults it to OFF)
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

/* ===================== Roles ===================== */
MERGE dbo.Roles AS target
USING (VALUES
    ('c2df441c-60ae-4504-be2e-ad642274438d', N'Buyer', N'BUYER', N'c2df441c-60ae-4504-be2e-ad642274438d'),
    ('52a7ae2c-d4ef-5c1d-3244-dbc3568581b8', N'Seller', N'SELLER', N'52a7ae2c-d4ef-5c1d-3244-dbc3568581b8'),
    ('ebfcd7bd-37d8-4470-7761-8e3ad52953ff', N'Admin', N'ADMIN', N'ebfcd7bd-37d8-4470-7761-8e3ad52953ff')
) AS source (Id, Name, NormalizedName, ConcurrencyStamp)
ON target.Id = source.Id
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (source.Id, source.Name, source.NormalizedName, source.ConcurrencyStamp);

/* ===================== States and union territories ===================== */
MERGE dbo.States AS target
USING (VALUES
    ('e6aa04f7-d5fd-f6e7-a513-318701d2b69e', N'Andhra Pradesh', 'AP', 0),
    ('6899a5f0-5206-0bf9-04ea-c578735191cf', N'Arunachal Pradesh', 'AR', 0),
    ('e9f7722c-d16e-d561-6662-9e1cc2908e51', N'Assam', 'AS', 0),
    ('60af8f51-8f2c-2754-7b31-bee5e7aa7458', N'Bihar', 'BR', 0),
    ('ffe432f4-b3ef-96b7-8485-c28563921172', N'Chhattisgarh', 'CG', 0),
    ('ee302fc8-ba00-9446-64e5-de2bcd5ffce2', N'Goa', 'GA', 0),
    ('c009e53c-9cab-477b-cf3b-ee304bcec2a1', N'Gujarat', 'GJ', 0),
    ('dd589f15-a5a3-cff0-90fa-a69bc5e80875', N'Haryana', 'HR', 0),
    ('9707807e-4b7b-9be9-5cec-af50b9bbaca7', N'Himachal Pradesh', 'HP', 0),
    ('cff95cf5-cd02-9c2c-f167-c636a3fd89d5', N'Jharkhand', 'JH', 0),
    ('2fce0f1b-3d7d-61c1-3a90-12dbc1af84b8', N'Karnataka', 'KA', 0),
    ('e5f30e5d-c556-4719-183c-9e3dd32cc61e', N'Kerala', 'KL', 0),
    ('b68dc2ef-dcee-2964-6fe2-8fe6d7ac17d0', N'Madhya Pradesh', 'MP', 0),
    ('0451bc22-4061-961f-667f-641ed63ec6e7', N'Maharashtra', 'MH', 0),
    ('87b810fc-4084-91af-d385-fc93bb09b8d3', N'Manipur', 'MN', 0),
    ('f33ee4df-461c-a533-8b70-d3c1e250c57c', N'Meghalaya', 'ML', 0),
    ('8ef11066-3c59-2cb5-310e-f2bf04f2d45a', N'Mizoram', 'MZ', 0),
    ('b9380da2-1d48-0f2c-957a-e7c26cdfef7a', N'Nagaland', 'NL', 0),
    ('c6caf7c7-4b68-e0e0-e94d-c891f7a4302e', N'Odisha', 'OD', 0),
    ('33525d80-827c-cda1-0bd2-ef38b1312f67', N'Punjab', 'PB', 0),
    ('549e1788-0049-1a0a-1184-582563a58cb2', N'Rajasthan', 'RJ', 0),
    ('5064891e-1f22-a356-d308-e351d3bdebd1', N'Sikkim', 'SK', 0),
    ('3163ad1b-8454-8f8a-6caa-a7915c402bb4', N'Tamil Nadu', 'TN', 0),
    ('31923913-0ddc-eca8-1ea3-da87ecc941ce', N'Telangana', 'TG', 0),
    ('00bbce74-f65e-5819-2e42-25a4fb8dda74', N'Tripura', 'TR', 0),
    ('20e11c17-de84-f77f-b82b-8d1e8ad1c46e', N'Uttar Pradesh', 'UP', 0),
    ('ea6e172d-386b-2fc2-d08a-faa5913b722e', N'Uttarakhand', 'UK', 0),
    ('dcfefe9f-0b39-7136-ca61-f939870ed43d', N'West Bengal', 'WB', 0),
    ('ca97f54e-24fa-4f38-d691-5c866d8612ad', N'Andaman and Nicobar Islands', 'AN', 1),
    ('8491bea0-b184-87a8-50ec-4cc887d75795', N'Chandigarh', 'CH', 1),
    ('4553afb6-f2f5-40bf-edee-88895bc19bb8', N'Dadra and Nagar Haveli and Daman and Diu', 'DH', 1),
    ('29541b7f-c7db-8747-a1df-5ae013f42446', N'Delhi', 'DL', 1),
    ('060e55a9-1c06-4761-8aa7-2f69be51f990', N'Jammu and Kashmir', 'JK', 1),
    ('1f82017b-0828-5bd9-7cc4-3c1db214781c', N'Ladakh', 'LA', 1),
    ('97e2999f-1f47-08e7-fb29-267a0e1e408c', N'Lakshadweep', 'LD', 1),
    ('e4d5d432-bdf9-8e19-fe4f-e6eae29c0b8c', N'Puducherry', 'PY', 1)
) AS source (Id, Name, Code, IsUnionTerritory)
ON target.Id = source.Id
WHEN MATCHED AND (target.Name <> source.Name OR target.Code <> source.Code OR target.IsUnionTerritory <> source.IsUnionTerritory) THEN
    UPDATE SET Name = source.Name, Code = source.Code, IsUnionTerritory = source.IsUnionTerritory, UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, Name, Code, IsUnionTerritory, CreatedAt, UpdatedAt)
    VALUES (source.Id, source.Name, source.Code, source.IsUnionTerritory, '2026-01-01', '2026-01-01');

/* ===================== Major cities ===================== */
MERGE dbo.Cities AS target
USING (VALUES
    -- Andhra Pradesh
    ('14c0fdb9-9833-cf5b-7eac-cc51d9278d93', 'e6aa04f7-d5fd-f6e7-a513-318701d2b69e', N'Visakhapatnam'),
    ('b75405d7-d52a-28c8-977a-4b1ad5d5274d', 'e6aa04f7-d5fd-f6e7-a513-318701d2b69e', N'Vijayawada'),
    ('198f8b8e-3a0f-f878-e77c-63ca59f48664', 'e6aa04f7-d5fd-f6e7-a513-318701d2b69e', N'Guntur'),
    ('c5ef8914-3e48-07e8-8286-e7453dcfb395', 'e6aa04f7-d5fd-f6e7-a513-318701d2b69e', N'Nellore'),
    ('d8518351-3842-ceb9-b83f-0ca2def2facd', 'e6aa04f7-d5fd-f6e7-a513-318701d2b69e', N'Tirupati'),
    ('aab25d4a-afc7-4f50-06f9-7d95e5045e95', 'e6aa04f7-d5fd-f6e7-a513-318701d2b69e', N'Kurnool'),
    -- Arunachal Pradesh
    ('faed5c60-1540-021f-7821-e98ceb61ba34', '6899a5f0-5206-0bf9-04ea-c578735191cf', N'Itanagar'),
    ('0a77d6a7-66d4-952a-3d06-fae9073dc49d', '6899a5f0-5206-0bf9-04ea-c578735191cf', N'Naharlagun'),
    ('5825a9bb-d16a-166e-cd20-3bd983717260', '6899a5f0-5206-0bf9-04ea-c578735191cf', N'Pasighat'),
    -- Assam
    ('f084a456-4fd6-7158-d22c-cb57f34cbee3', 'e9f7722c-d16e-d561-6662-9e1cc2908e51', N'Guwahati'),
    ('be50f758-e81f-4e70-317f-447fcea3316b', 'e9f7722c-d16e-d561-6662-9e1cc2908e51', N'Dibrugarh'),
    ('c5b3074f-7d92-bb6b-3a0e-cc35463e8249', 'e9f7722c-d16e-d561-6662-9e1cc2908e51', N'Silchar'),
    ('0a2037c3-ea47-eb11-c7b0-dd9accaa7f15', 'e9f7722c-d16e-d561-6662-9e1cc2908e51', N'Jorhat'),
    ('a387d875-48ec-8426-3b85-5da0a74264c8', 'e9f7722c-d16e-d561-6662-9e1cc2908e51', N'Tezpur'),
    -- Bihar
    ('49abdd68-0698-c40e-f813-dec06eece374', '60af8f51-8f2c-2754-7b31-bee5e7aa7458', N'Patna'),
    ('3c51036a-0202-a8d3-26b5-8cec39656930', '60af8f51-8f2c-2754-7b31-bee5e7aa7458', N'Gaya'),
    ('091e591b-1dac-fe49-90a0-6839663f4e69', '60af8f51-8f2c-2754-7b31-bee5e7aa7458', N'Bhagalpur'),
    ('9ebf021a-5d58-f706-69d0-120dc29ee6ed', '60af8f51-8f2c-2754-7b31-bee5e7aa7458', N'Muzaffarpur'),
    ('f4950e1a-626d-0252-969e-7a5c56c7aab7', '60af8f51-8f2c-2754-7b31-bee5e7aa7458', N'Darbhanga'),
    -- Chhattisgarh
    ('e72d5c81-0a84-4fc8-d558-ed9ddeaa40ac', 'ffe432f4-b3ef-96b7-8485-c28563921172', N'Raipur'),
    ('ad493ed4-f1af-0fd4-b961-382a1de97718', 'ffe432f4-b3ef-96b7-8485-c28563921172', N'Bhilai'),
    ('253d3457-76fa-8c7d-edb8-247bcbf8cc9a', 'ffe432f4-b3ef-96b7-8485-c28563921172', N'Bilaspur'),
    ('81428416-c828-cf9c-d10f-12cd8e71c4a3', 'ffe432f4-b3ef-96b7-8485-c28563921172', N'Korba'),
    ('4a3ce47f-0fb2-7830-f1b9-45e6a76403f7', 'ffe432f4-b3ef-96b7-8485-c28563921172', N'Durg'),
    -- Goa
    ('547af6ea-7cd2-9721-1c6c-83ad8cb02bc3', 'ee302fc8-ba00-9446-64e5-de2bcd5ffce2', N'Panaji'),
    ('09aa9844-8a54-9a71-8960-29a9ba7fcf64', 'ee302fc8-ba00-9446-64e5-de2bcd5ffce2', N'Margao'),
    ('68dd6dee-565a-f43f-bdd7-a37a63088626', 'ee302fc8-ba00-9446-64e5-de2bcd5ffce2', N'Vasco da Gama'),
    ('9be9e6ab-89e3-d466-5597-1abe27bb1f6a', 'ee302fc8-ba00-9446-64e5-de2bcd5ffce2', N'Mapusa'),
    -- Gujarat
    ('119783ab-a3ad-c8f4-4028-0e0c3272d2b6', 'c009e53c-9cab-477b-cf3b-ee304bcec2a1', N'Ahmedabad'),
    ('8b703557-d926-74f0-006e-e18de5ae2d9c', 'c009e53c-9cab-477b-cf3b-ee304bcec2a1', N'Surat'),
    ('20039599-6d21-4de0-4993-774ebb380acd', 'c009e53c-9cab-477b-cf3b-ee304bcec2a1', N'Vadodara'),
    ('08a8f1f6-0c8f-c675-d834-c1a492f72c04', 'c009e53c-9cab-477b-cf3b-ee304bcec2a1', N'Rajkot'),
    ('b5613229-2082-e51f-9613-c74992a4261d', 'c009e53c-9cab-477b-cf3b-ee304bcec2a1', N'Bhavnagar'),
    ('848c09b9-9ba0-d779-d888-4e1de10e5835', 'c009e53c-9cab-477b-cf3b-ee304bcec2a1', N'Jamnagar'),
    ('cfa1fe12-bedd-530c-a53a-3ea61aa95aac', 'c009e53c-9cab-477b-cf3b-ee304bcec2a1', N'Gandhinagar'),
    -- Haryana
    ('2fb58eda-562b-8804-37bc-230aa0f37f51', 'dd589f15-a5a3-cff0-90fa-a69bc5e80875', N'Gurugram'),
    ('466db721-2261-eb5d-c046-786532f14967', 'dd589f15-a5a3-cff0-90fa-a69bc5e80875', N'Faridabad'),
    ('71558e42-a632-38ff-fcb7-cd4f464e1f48', 'dd589f15-a5a3-cff0-90fa-a69bc5e80875', N'Panipat'),
    ('e2ee8ac5-733f-6774-c8be-381d4fc62c5a', 'dd589f15-a5a3-cff0-90fa-a69bc5e80875', N'Ambala'),
    ('608df590-fd80-a554-e9c7-f983981cb472', 'dd589f15-a5a3-cff0-90fa-a69bc5e80875', N'Karnal'),
    ('3abc8541-8e16-083a-c0f7-5a81077ac191', 'dd589f15-a5a3-cff0-90fa-a69bc5e80875', N'Sonipat'),
    -- Himachal Pradesh
    ('9924255e-d496-e49d-7775-65d5abb80e25', '9707807e-4b7b-9be9-5cec-af50b9bbaca7', N'Shimla'),
    ('4bf520b5-39a1-1f50-d3c5-467027f6b705', '9707807e-4b7b-9be9-5cec-af50b9bbaca7', N'Baddi'),
    ('894a1a2d-9c29-6d4f-a572-66d7cdf4a442', '9707807e-4b7b-9be9-5cec-af50b9bbaca7', N'Solan'),
    ('a366c15e-a421-eccc-a7b5-3cb4fbfb5972', '9707807e-4b7b-9be9-5cec-af50b9bbaca7', N'Dharamshala'),
    ('2a80c3f8-dac8-40c1-948a-eba334750917', '9707807e-4b7b-9be9-5cec-af50b9bbaca7', N'Mandi'),
    -- Jharkhand
    ('5c417495-8874-8e81-e5ad-9d578db0345d', 'cff95cf5-cd02-9c2c-f167-c636a3fd89d5', N'Ranchi'),
    ('55950d51-cc6e-31b0-4569-1164339512e5', 'cff95cf5-cd02-9c2c-f167-c636a3fd89d5', N'Jamshedpur'),
    ('1b42a959-f5a3-486e-6758-98b26ea0eabc', 'cff95cf5-cd02-9c2c-f167-c636a3fd89d5', N'Dhanbad'),
    ('62ad8a29-f180-4457-a457-acfc94879fa5', 'cff95cf5-cd02-9c2c-f167-c636a3fd89d5', N'Bokaro Steel City'),
    ('28a69481-2e20-77f7-da29-7f29e3818280', 'cff95cf5-cd02-9c2c-f167-c636a3fd89d5', N'Hazaribagh'),
    -- Karnataka
    ('4240d101-34d4-7698-a356-33cb47022e53', '2fce0f1b-3d7d-61c1-3a90-12dbc1af84b8', N'Bengaluru'),
    ('3f216dc6-c194-483f-3b5a-925c6121d342', '2fce0f1b-3d7d-61c1-3a90-12dbc1af84b8', N'Mysuru'),
    ('6b6bed4f-0ef9-eb21-8d7d-49c215fdc447', '2fce0f1b-3d7d-61c1-3a90-12dbc1af84b8', N'Hubballi'),
    ('c4643f4d-aa6b-3956-b2da-c42791f43513', '2fce0f1b-3d7d-61c1-3a90-12dbc1af84b8', N'Mangaluru'),
    ('6796e9e9-8d25-638c-a9ad-d4622ad62886', '2fce0f1b-3d7d-61c1-3a90-12dbc1af84b8', N'Belagavi'),
    ('ade3af8f-cc4c-c12e-f6c0-0bb40042fc00', '2fce0f1b-3d7d-61c1-3a90-12dbc1af84b8', N'Kalaburagi'),
    -- Kerala
    ('22ee6cd5-7697-3140-9215-6960a44b2951', 'e5f30e5d-c556-4719-183c-9e3dd32cc61e', N'Kochi'),
    ('4300cca8-c112-8f80-a40a-314ea2d9cef6', 'e5f30e5d-c556-4719-183c-9e3dd32cc61e', N'Thiruvananthapuram'),
    ('e62a0b6d-3493-849b-950e-c495e09d2229', 'e5f30e5d-c556-4719-183c-9e3dd32cc61e', N'Kozhikode'),
    ('f7f0613e-c132-ae0e-96e8-1e1fdf398b1b', 'e5f30e5d-c556-4719-183c-9e3dd32cc61e', N'Thrissur'),
    ('70cd7091-fed7-93b1-9bb5-6231b8220a81', 'e5f30e5d-c556-4719-183c-9e3dd32cc61e', N'Kollam'),
    -- Madhya Pradesh
    ('2e89e1f8-5354-7a54-cb37-411708d065e6', 'b68dc2ef-dcee-2964-6fe2-8fe6d7ac17d0', N'Indore'),
    ('5f8d6361-46de-ab67-7919-2358f54feedc', 'b68dc2ef-dcee-2964-6fe2-8fe6d7ac17d0', N'Bhopal'),
    ('6d5eea6a-5798-b63b-38fd-c4adf40ca9f3', 'b68dc2ef-dcee-2964-6fe2-8fe6d7ac17d0', N'Jabalpur'),
    ('4ab462ac-f759-6c02-4ce9-a8b42eea2c90', 'b68dc2ef-dcee-2964-6fe2-8fe6d7ac17d0', N'Gwalior'),
    ('482df0a3-3776-97f2-2fd0-6d844091bb9b', 'b68dc2ef-dcee-2964-6fe2-8fe6d7ac17d0', N'Ujjain'),
    -- Maharashtra
    ('cd0561ae-342d-7f86-5c01-b32ec44385f9', '0451bc22-4061-961f-667f-641ed63ec6e7', N'Mumbai'),
    ('30c254e6-05f2-5d61-56e3-e9bb1b806901', '0451bc22-4061-961f-667f-641ed63ec6e7', N'Pune'),
    ('e86b82a5-b13f-9931-90f9-470e117326e9', '0451bc22-4061-961f-667f-641ed63ec6e7', N'Nagpur'),
    ('f0dbd2b7-f865-249b-2ec9-19b542770e67', '0451bc22-4061-961f-667f-641ed63ec6e7', N'Thane'),
    ('f707c3d7-de9c-ddb9-203c-e076ae891ed3', '0451bc22-4061-961f-667f-641ed63ec6e7', N'Nashik'),
    ('eabaabff-c7dd-14c4-f828-9c5d80ea9afd', '0451bc22-4061-961f-667f-641ed63ec6e7', N'Chhatrapati Sambhajinagar'),
    ('edba4be2-ac2a-5156-c4db-532f4409e0f3', '0451bc22-4061-961f-667f-641ed63ec6e7', N'Kolhapur'),
    ('6187685e-4bd1-f959-de17-aaa07ab54385', '0451bc22-4061-961f-667f-641ed63ec6e7', N'Solapur'),
    -- Manipur
    ('600bb65e-92e8-4c0f-defc-8d972d2e18d9', '87b810fc-4084-91af-d385-fc93bb09b8d3', N'Imphal'),
    ('553d7ec5-1324-5e30-9f1b-fb8f397529a0', '87b810fc-4084-91af-d385-fc93bb09b8d3', N'Thoubal'),
    ('699c5aaa-a967-1d0d-8d95-fd6050489f82', '87b810fc-4084-91af-d385-fc93bb09b8d3', N'Churachandpur'),
    -- Meghalaya
    ('e74c66fc-3d28-5ebd-58a3-764a93c11de0', 'f33ee4df-461c-a533-8b70-d3c1e250c57c', N'Shillong'),
    ('ea956939-ad8b-abf6-6992-cd61ee7e7f37', 'f33ee4df-461c-a533-8b70-d3c1e250c57c', N'Tura'),
    ('a0e82429-ffd9-a89b-af58-4c267e04e9c4', 'f33ee4df-461c-a533-8b70-d3c1e250c57c', N'Jowai'),
    -- Mizoram
    ('5683eee7-7469-edd8-7770-208888dfb01c', '8ef11066-3c59-2cb5-310e-f2bf04f2d45a', N'Aizawl'),
    ('8fc4caef-eb4c-c165-ba91-8fbe034b612a', '8ef11066-3c59-2cb5-310e-f2bf04f2d45a', N'Lunglei'),
    ('77778ff4-ddd4-2596-a787-b1eeabb3a909', '8ef11066-3c59-2cb5-310e-f2bf04f2d45a', N'Champhai'),
    -- Nagaland
    ('d6b2d127-d2a3-574e-ddb2-30af494cd0c2', 'b9380da2-1d48-0f2c-957a-e7c26cdfef7a', N'Kohima'),
    ('561e3701-22a0-16b2-3e8d-e59bf24026cd', 'b9380da2-1d48-0f2c-957a-e7c26cdfef7a', N'Dimapur'),
    ('bf027b35-9b45-0ace-ec28-30d48300bbcf', 'b9380da2-1d48-0f2c-957a-e7c26cdfef7a', N'Mokokchung'),
    -- Odisha
    ('faa5ffcc-c27d-9ad0-c9f3-c15595dd5c58', 'c6caf7c7-4b68-e0e0-e94d-c891f7a4302e', N'Bhubaneswar'),
    ('414e49b8-36e8-753d-8280-406c8ca736dd', 'c6caf7c7-4b68-e0e0-e94d-c891f7a4302e', N'Cuttack'),
    ('3e043b39-546c-ab13-fec7-269fdcc1a4c1', 'c6caf7c7-4b68-e0e0-e94d-c891f7a4302e', N'Rourkela'),
    ('1b5c71f8-f0b4-9994-7bb5-2b60808e9e5f', 'c6caf7c7-4b68-e0e0-e94d-c891f7a4302e', N'Berhampur'),
    ('ebdd9772-7a41-c918-c3bf-985afcbea2a5', 'c6caf7c7-4b68-e0e0-e94d-c891f7a4302e', N'Sambalpur'),
    -- Punjab
    ('d26a01d1-d179-c079-86ed-2d1026178688', '33525d80-827c-cda1-0bd2-ef38b1312f67', N'Ludhiana'),
    ('76a8b561-f691-b7c5-c548-66a41f7fc007', '33525d80-827c-cda1-0bd2-ef38b1312f67', N'Amritsar'),
    ('385ed600-ac68-0f64-8bb7-49205e731fd4', '33525d80-827c-cda1-0bd2-ef38b1312f67', N'Jalandhar'),
    ('c4e05d09-2486-126f-8961-ae81dff27b57', '33525d80-827c-cda1-0bd2-ef38b1312f67', N'Mohali'),
    ('a345e499-0caa-9f91-413b-e6103db079d5', '33525d80-827c-cda1-0bd2-ef38b1312f67', N'Patiala'),
    ('6f795b02-08e3-c584-791e-8931c46f7764', '33525d80-827c-cda1-0bd2-ef38b1312f67', N'Bathinda'),
    -- Rajasthan
    ('e05a6301-d9a7-5ec0-467d-6a204b2513dc', '549e1788-0049-1a0a-1184-582563a58cb2', N'Jaipur'),
    ('d22e8123-223e-f55c-d0a1-ed47342c0a9a', '549e1788-0049-1a0a-1184-582563a58cb2', N'Jodhpur'),
    ('3069449c-67df-e119-5bb8-ea1d1299550e', '549e1788-0049-1a0a-1184-582563a58cb2', N'Udaipur'),
    ('11f9a97c-855b-c71b-6989-64f52a33db94', '549e1788-0049-1a0a-1184-582563a58cb2', N'Kota'),
    ('09a8b6f4-a847-ce72-b72b-a0144513cade', '549e1788-0049-1a0a-1184-582563a58cb2', N'Ajmer'),
    ('c5d9203a-b732-455a-fefb-da70e418d316', '549e1788-0049-1a0a-1184-582563a58cb2', N'Bhilwara'),
    ('29567872-c0c6-a3d6-c896-f81dcca7d1a3', '549e1788-0049-1a0a-1184-582563a58cb2', N'Bikaner'),
    -- Sikkim
    ('f402263e-dcc1-8712-2e2a-078e9bf5b5c6', '5064891e-1f22-a356-d308-e351d3bdebd1', N'Gangtok'),
    ('5858aea5-5aee-2ee7-a202-6a5762b482e0', '5064891e-1f22-a356-d308-e351d3bdebd1', N'Namchi'),
    ('2c54d270-6b65-021a-f01f-f91098e2dfd0', '5064891e-1f22-a356-d308-e351d3bdebd1', N'Gyalshing'),
    -- Tamil Nadu
    ('c0262cc7-8aea-7363-89ae-d7b25d80c1f6', '3163ad1b-8454-8f8a-6caa-a7915c402bb4', N'Chennai'),
    ('de0e0487-bf2e-53df-1193-aa1148bd0d32', '3163ad1b-8454-8f8a-6caa-a7915c402bb4', N'Coimbatore'),
    ('6233ad32-beab-73b9-92a0-00734e6bcf73', '3163ad1b-8454-8f8a-6caa-a7915c402bb4', N'Madurai'),
    ('1e754f24-648a-201a-9fb7-a7b789694ba5', '3163ad1b-8454-8f8a-6caa-a7915c402bb4', N'Tiruppur'),
    ('6940f2c1-dc40-2394-74d7-acdd7e4f22f6', '3163ad1b-8454-8f8a-6caa-a7915c402bb4', N'Salem'),
    ('bbda81bd-d741-cbd3-e07c-6994597d3df1', '3163ad1b-8454-8f8a-6caa-a7915c402bb4', N'Tiruchirappalli'),
    -- Telangana
    ('7ba8c520-3e2c-fabb-c45e-32a1517d1054', '31923913-0ddc-eca8-1ea3-da87ecc941ce', N'Hyderabad'),
    ('10098e4f-30f7-163a-feaa-8f5a764045ba', '31923913-0ddc-eca8-1ea3-da87ecc941ce', N'Warangal'),
    ('80c296a1-d15b-f4de-adce-80ea8777c93e', '31923913-0ddc-eca8-1ea3-da87ecc941ce', N'Nizamabad'),
    ('13a92c9e-cb1f-b4ef-f707-625ba10fd1c8', '31923913-0ddc-eca8-1ea3-da87ecc941ce', N'Karimnagar'),
    ('7d8f10b9-88bb-bb83-8e5a-86039c38bbad', '31923913-0ddc-eca8-1ea3-da87ecc941ce', N'Khammam'),
    -- Tripura
    ('ed76b7c9-e6a7-5d9e-a83f-04220820b12e', '00bbce74-f65e-5819-2e42-25a4fb8dda74', N'Agartala'),
    ('d05983a3-7de2-129a-94ee-15435c030a08', '00bbce74-f65e-5819-2e42-25a4fb8dda74', N'Udaipur'),
    ('ba98a682-4e82-5189-0799-bf58bfe346b7', '00bbce74-f65e-5819-2e42-25a4fb8dda74', N'Dharmanagar'),
    -- Uttar Pradesh
    ('1570d630-627a-de5b-ebc9-2f47b7ed4dc5', '20e11c17-de84-f77f-b82b-8d1e8ad1c46e', N'Lucknow'),
    ('b7dee2e5-5e71-52be-51e9-598271ff3618', '20e11c17-de84-f77f-b82b-8d1e8ad1c46e', N'Kanpur'),
    ('72c371e1-079d-9d6d-0a16-1e87a2ed7223', '20e11c17-de84-f77f-b82b-8d1e8ad1c46e', N'Noida'),
    ('c334c4fa-887b-12dd-de3b-1d3ac2fb6c20', '20e11c17-de84-f77f-b82b-8d1e8ad1c46e', N'Ghaziabad'),
    ('0214504a-6d4d-b995-737c-2f66dd753b6f', '20e11c17-de84-f77f-b82b-8d1e8ad1c46e', N'Agra'),
    ('4e355cbd-5861-eff4-70c1-94e7ebe34f9f', '20e11c17-de84-f77f-b82b-8d1e8ad1c46e', N'Varanasi'),
    ('70fbfce7-25db-cafd-8c6f-0d250c92b1fe', '20e11c17-de84-f77f-b82b-8d1e8ad1c46e', N'Prayagraj'),
    ('8153a689-ebf2-9693-5b9d-dfb02d38d297', '20e11c17-de84-f77f-b82b-8d1e8ad1c46e', N'Meerut'),
    ('e60114f8-9041-c3cf-bc8a-8016982c1095', '20e11c17-de84-f77f-b82b-8d1e8ad1c46e', N'Aligarh'),
    ('2f87a252-0729-e0a4-4748-3e69434f5c72', '20e11c17-de84-f77f-b82b-8d1e8ad1c46e', N'Moradabad'),
    -- Uttarakhand
    ('e0167e86-1fa3-a960-6786-44b5c30e716a', 'ea6e172d-386b-2fc2-d08a-faa5913b722e', N'Dehradun'),
    ('c25e3177-ef33-f1d4-0433-8d1198616fac', 'ea6e172d-386b-2fc2-d08a-faa5913b722e', N'Haridwar'),
    ('0e515039-caa7-e3af-a400-e83855190e47', 'ea6e172d-386b-2fc2-d08a-faa5913b722e', N'Rudrapur'),
    ('8dda462e-3f25-ebc7-3c4f-bc3a85719afb', 'ea6e172d-386b-2fc2-d08a-faa5913b722e', N'Haldwani'),
    ('35fc9426-b65d-2bfb-637b-bf8ee6ff9856', 'ea6e172d-386b-2fc2-d08a-faa5913b722e', N'Roorkee'),
    -- West Bengal
    ('59d4dba9-f0c6-7543-3269-3c5e7765aa16', 'dcfefe9f-0b39-7136-ca61-f939870ed43d', N'Kolkata'),
    ('a4708397-1260-b563-11f5-6be034b6d0a1', 'dcfefe9f-0b39-7136-ca61-f939870ed43d', N'Howrah'),
    ('bb04d359-352f-6c2e-58e9-04f171c33210', 'dcfefe9f-0b39-7136-ca61-f939870ed43d', N'Durgapur'),
    ('81a53acb-eb99-e778-319c-2bd8847f793a', 'dcfefe9f-0b39-7136-ca61-f939870ed43d', N'Asansol'),
    ('3c5d0df0-36c7-ea72-f8a0-2330c6059ab9', 'dcfefe9f-0b39-7136-ca61-f939870ed43d', N'Siliguri'),
    ('2f0b1944-3fc9-6949-1de5-38dbbbd6364a', 'dcfefe9f-0b39-7136-ca61-f939870ed43d', N'Haldia'),
    -- Andaman and Nicobar Islands
    ('119cf648-200f-de0d-9a76-630dc47b6b50', 'ca97f54e-24fa-4f38-d691-5c866d8612ad', N'Sri Vijaya Puram (Port Blair)'),
    -- Chandigarh
    ('fd6f5ab5-58a3-d706-4b40-0c3237ce75e2', '8491bea0-b184-87a8-50ec-4cc887d75795', N'Chandigarh'),
    -- Dadra and Nagar Haveli and Daman and Diu
    ('9f96f454-51a6-c330-146a-16148c12e4e1', '4553afb6-f2f5-40bf-edee-88895bc19bb8', N'Daman'),
    ('e57b7603-9bbc-9fbf-e4d5-dad39615d650', '4553afb6-f2f5-40bf-edee-88895bc19bb8', N'Diu'),
    ('c0e3a41c-02d7-3b62-14c3-3b6389df3263', '4553afb6-f2f5-40bf-edee-88895bc19bb8', N'Silvassa'),
    -- Delhi
    ('8b68a4cd-fe01-61a3-337d-141128f032d6', '29541b7f-c7db-8747-a1df-5ae013f42446', N'New Delhi'),
    ('ceac8e0b-b5a5-6a63-438b-3444794b2af0', '29541b7f-c7db-8747-a1df-5ae013f42446', N'Delhi'),
    -- Jammu and Kashmir
    ('b6320d01-1acc-d18f-cd0f-0683055a6526', '060e55a9-1c06-4761-8aa7-2f69be51f990', N'Srinagar'),
    ('82e622db-f1b9-a114-b4a5-2a6f10989459', '060e55a9-1c06-4761-8aa7-2f69be51f990', N'Jammu'),
    ('534ca7ef-4327-ec54-0c4d-11cf3292b7ff', '060e55a9-1c06-4761-8aa7-2f69be51f990', N'Kathua'),
    ('a2db5e57-6fc6-ded5-272b-1e9eaba8e3e0', '060e55a9-1c06-4761-8aa7-2f69be51f990', N'Anantnag'),
    -- Ladakh
    ('ae8f7912-5aa0-cb46-23ad-aba1534ae47c', '1f82017b-0828-5bd9-7cc4-3c1db214781c', N'Leh'),
    ('3b86132a-8219-c045-f9c6-329f92e5bd02', '1f82017b-0828-5bd9-7cc4-3c1db214781c', N'Kargil'),
    -- Lakshadweep
    ('ca2727e8-e4cb-461c-bcdb-ad14dba11e6d', '97e2999f-1f47-08e7-fb29-267a0e1e408c', N'Kavaratti'),
    -- Puducherry
    ('7fa73c18-4e87-88dc-eb14-8a249f14d0c6', 'e4d5d432-bdf9-8e19-fe4f-e6eae29c0b8c', N'Puducherry'),
    ('217ad10f-3e76-0d9e-73b6-9432e8417655', 'e4d5d432-bdf9-8e19-fe4f-e6eae29c0b8c', N'Karaikal'),
    ('38809cff-15c1-7680-cdc9-aef40a2956d5', 'e4d5d432-bdf9-8e19-fe4f-e6eae29c0b8c', N'Yanam'),
    ('8b9048f3-4ce7-e555-c4fa-5dae79ed3306', 'e4d5d432-bdf9-8e19-fe4f-e6eae29c0b8c', N'Mahe')
) AS source (Id, StateId, Name)
ON target.Id = source.Id
WHEN MATCHED AND (target.Name <> source.Name OR target.StateId <> source.StateId) THEN
    UPDATE SET Name = source.Name, StateId = source.StateId, UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, StateId, Name, CreatedAt, UpdatedAt)
    VALUES (source.Id, source.StateId, source.Name, '2026-01-01', '2026-01-01');

/* ===================== Category tree (top level, then subcategories) ===================== */
MERGE dbo.Categories AS target
USING (VALUES
    ('5a47e4bb-da67-d97a-feab-187e1839d791', NULL, N'Textiles', 'textiles', 1),
    ('910ffe5b-d6e0-77cf-3ccc-25cba668710c', '5a47e4bb-da67-d97a-feab-187e1839d791', N'Yarn & Fibre', 'yarn-fibre', 1),
    ('a46e025a-93db-ab05-a3de-a886403b9a76', '5a47e4bb-da67-d97a-feab-187e1839d791', N'Fabrics', 'fabrics', 2),
    ('35b8e70d-4eb0-4cc0-b467-3ef8ecc3cb5c', '5a47e4bb-da67-d97a-feab-187e1839d791', N'Garments & Apparel', 'garments-apparel', 3),
    ('3cf404bc-4b01-eff7-ec99-4b8125119851', '5a47e4bb-da67-d97a-feab-187e1839d791', N'Home Textiles', 'home-textiles', 4),
    ('bca85f05-c040-1b7b-ad3e-a5fe737f6534', '5a47e4bb-da67-d97a-feab-187e1839d791', N'Technical Textiles', 'technical-textiles', 5),
    ('3b993514-b6b0-2d55-2f5d-e5a8edc8f878', NULL, N'Agriculture', 'agriculture', 2),
    ('a55249d9-c92e-9f8c-7196-3fb8e1532929', '3b993514-b6b0-2d55-2f5d-e5a8edc8f878', N'Grains & Pulses', 'grains-pulses', 1),
    ('56f1c783-0190-f9e8-0d1c-ec7479e69504', '3b993514-b6b0-2d55-2f5d-e5a8edc8f878', N'Spices', 'spices', 2),
    ('f0f867b4-5d1c-8ab8-2750-ec90248774b3', '3b993514-b6b0-2d55-2f5d-e5a8edc8f878', N'Fruits & Vegetables', 'fruits-vegetables', 3),
    ('0fad1db6-a963-8eaf-18e7-dd5dacc56847', '3b993514-b6b0-2d55-2f5d-e5a8edc8f878', N'Seeds', 'seeds', 4),
    ('e818454a-c080-850f-c75b-bbbd041ab371', '3b993514-b6b0-2d55-2f5d-e5a8edc8f878', N'Fertilizers & Pesticides', 'fertilizers-pesticides', 5),
    ('21760484-1ff3-9fbb-c0b5-2fbaa1dd6daf', NULL, N'Electronics', 'electronics', 3),
    ('ba8fa7d7-8cdc-5f67-8a11-84ca871f1a49', '21760484-1ff3-9fbb-c0b5-2fbaa1dd6daf', N'Electronic Components', 'electronic-components', 1),
    ('7fc9b3de-db9e-a634-ba57-b2d70874636b', '21760484-1ff3-9fbb-c0b5-2fbaa1dd6daf', N'Consumer Electronics', 'consumer-electronics', 2),
    ('d013ff41-42c2-aae2-fd3e-faeefcc187c8', '21760484-1ff3-9fbb-c0b5-2fbaa1dd6daf', N'Electrical Equipment', 'electrical-equipment', 3),
    ('2c4343fe-9a27-2d18-c69f-8f5325ec14f1', '21760484-1ff3-9fbb-c0b5-2fbaa1dd6daf', N'Lighting', 'lighting', 4),
    ('5cf7913d-eab9-df63-ea9e-9d00ae942d42', '21760484-1ff3-9fbb-c0b5-2fbaa1dd6daf', N'Solar & Power', 'solar-power', 5),
    ('4f6d9ac8-c60e-b57f-0492-6b77ac58a3a3', NULL, N'Machinery', 'machinery', 4),
    ('9f27091b-4dca-6912-890a-4171dcc9c518', '4f6d9ac8-c60e-b57f-0492-6b77ac58a3a3', N'Industrial Machinery', 'industrial-machinery', 1),
    ('1f35e0b7-8da8-cda1-8e03-99ef76c42867', '4f6d9ac8-c60e-b57f-0492-6b77ac58a3a3', N'Agricultural Machinery', 'agricultural-machinery', 2),
    ('43e599d0-beee-eb94-a210-f4d6fb6ad1b6', '4f6d9ac8-c60e-b57f-0492-6b77ac58a3a3', N'Construction Machinery', 'construction-machinery', 3),
    ('fd857ed1-81b6-a24c-75b4-463c25b7252f', '4f6d9ac8-c60e-b57f-0492-6b77ac58a3a3', N'Machine Tools', 'machine-tools', 4),
    ('32460944-6f60-7d48-0dde-8ca1226fa3fc', '4f6d9ac8-c60e-b57f-0492-6b77ac58a3a3', N'Pumps & Motors', 'pumps-motors', 5),
    ('05d3cf71-0d10-1997-5a96-812ed2a77f95', NULL, N'Chemicals', 'chemicals', 5),
    ('ec170093-aebf-ae85-f551-fced7488b274', '05d3cf71-0d10-1997-5a96-812ed2a77f95', N'Industrial Chemicals', 'industrial-chemicals', 1),
    ('2a43014b-04a1-c7f7-d9ee-8fb3d0934efe', '05d3cf71-0d10-1997-5a96-812ed2a77f95', N'Dyes & Pigments', 'dyes-pigments', 2),
    ('c0922f3f-e062-2418-9079-326e78289790', '05d3cf71-0d10-1997-5a96-812ed2a77f95', N'Plastics & Polymers', 'plastics-polymers', 3),
    ('ee89bebb-d6ff-08ce-b7c1-ba7b7c563846', '05d3cf71-0d10-1997-5a96-812ed2a77f95', N'Paints & Coatings', 'paints-coatings', 4),
    ('b9a420ad-4469-176e-b10c-4fb8bb5e413b', '05d3cf71-0d10-1997-5a96-812ed2a77f95', N'Pharmaceutical Chemicals', 'pharmaceutical-chemicals', 5),
    ('3269a099-24f1-9152-1bcd-c2647f243d40', NULL, N'Packaging', 'packaging', 6),
    ('0286aa0c-eae8-3879-81cf-7c41e8b63724', '3269a099-24f1-9152-1bcd-c2647f243d40', N'Corrugated Boxes', 'corrugated-boxes', 1),
    ('fd95b6d5-905f-c052-23f9-a389e2ec074c', '3269a099-24f1-9152-1bcd-c2647f243d40', N'Flexible Packaging', 'flexible-packaging', 2),
    ('77e82352-61a8-c63f-b0aa-021c7be064ce', '3269a099-24f1-9152-1bcd-c2647f243d40', N'Plastic Containers', 'plastic-containers', 3),
    ('d285dad4-85ae-0fc8-4104-395442a6497a', '3269a099-24f1-9152-1bcd-c2647f243d40', N'Labels & Tapes', 'labels-tapes', 4),
    ('f3b07645-fd82-6085-c1b0-738876f0ff94', NULL, N'Food & Beverage', 'food-beverage', 7),
    ('77ffd4e3-0d24-936f-2f12-744f49246a9c', 'f3b07645-fd82-6085-c1b0-738876f0ff94', N'Processed Foods', 'processed-foods', 1),
    ('0ee6a923-5dc8-ba0b-3009-6a03dc1f2800', 'f3b07645-fd82-6085-c1b0-738876f0ff94', N'Dairy Products', 'dairy-products', 2),
    ('983fa102-d471-f3f3-a7bb-33f39715be82', 'f3b07645-fd82-6085-c1b0-738876f0ff94', N'Beverages', 'beverages', 3),
    ('7a299e6f-bf57-5ea6-8dfe-a3eedad0767b', 'f3b07645-fd82-6085-c1b0-738876f0ff94', N'Edible Oils', 'edible-oils', 4),
    ('2daabd19-c8af-bc66-1270-7afb96a0fa73', 'f3b07645-fd82-6085-c1b0-738876f0ff94', N'Bakery & Confectionery', 'bakery-confectionery', 5),
    ('9be26e49-d3dd-2641-a540-107805c15477', NULL, N'Construction Materials', 'construction-materials', 8),
    ('0f3a8cab-7928-506d-04b9-e528f64163e0', '9be26e49-d3dd-2641-a540-107805c15477', N'Cement & Concrete', 'cement-concrete', 1),
    ('0c8e1ffb-3fc0-5841-f20f-c3eb527c7770', '9be26e49-d3dd-2641-a540-107805c15477', N'Steel & TMT Bars', 'steel-tmt-bars', 2),
    ('00db4eb9-5150-0f95-8c12-5a2c173b1d18', '9be26e49-d3dd-2641-a540-107805c15477', N'Bricks & Blocks', 'bricks-blocks', 3),
    ('99521e2d-2047-809b-b486-dfb35b0284a6', '9be26e49-d3dd-2641-a540-107805c15477', N'Tiles & Sanitaryware', 'tiles-sanitaryware', 4),
    ('899339e5-01f9-4ac3-e8bf-990d6b2b0bd3', '9be26e49-d3dd-2641-a540-107805c15477', N'Pipes & Fittings', 'pipes-fittings', 5)
) AS source (Id, ParentId, Name, Slug, SortOrder)
ON target.Id = source.Id
WHEN MATCHED AND (target.Name <> source.Name OR target.Slug <> source.Slug OR target.SortOrder <> source.SortOrder
                  OR ISNULL(target.ParentId, '00000000-0000-0000-0000-000000000000') <> ISNULL(source.ParentId, '00000000-0000-0000-0000-000000000000')) THEN
    UPDATE SET ParentId = source.ParentId, Name = source.Name, Slug = source.Slug, SortOrder = source.SortOrder, UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, ParentId, Name, Slug, SortOrder, IsActive, CreatedAt, UpdatedAt)
    VALUES (source.Id, source.ParentId, source.Name, source.Slug, source.SortOrder, 1, '2026-01-01', '2026-01-01');

COMMIT TRANSACTION;
GO

PRINT N'02_SeedData.sql completed.';
GO
