-- ============================================================
-- Company dashboard test data
-- company  = 7  (gio.company@bewegdeal.at)
-- customer = 6  (gio.customer@bewegdeal.at)
-- ============================================================

INSERT INTO dev_Requests
  (Number, CreateDate, Status, Service, Title, Description,
   PickupAddress, DeliveryAddress,
   RequesterId, ExecutorId, Cost, Currency, ASAP, Date, Time, AgreementId)
VALUES

-- ── January 2026 ─────────────────────────────────────────────────────────
(REPLACE(UUID(),'-',''), '2026-01-04 09:15:00', 'resolved',    'moving',    'Apartment Move – Mariahilf',      'Full relocation of 3-room apartment including furniture assembly.',     'Mariahilfer Str. 12, 1060 Vienna',   'Favoritenstr. 55, 1100 Vienna',      6, 7, 920.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-01-09 11:00:00', 'resolved',    'removal',   'Junk Clearance – Penzing',        'Basement clearance, old furniture and appliances.',                     'Linzer Str. 88, 1140 Vienna',        'Linzer Str. 88, 1140 Vienna',        6, 7, 340.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-01-14 14:30:00', 'resolved',    'pickup',    'IKEA Store Pickup',               'Pickup and delivery of flat-pack furniture order.',                     'IKEA Vienna Vösendorf',              'Quellenstr. 20, 1100 Vienna',        6, 7, 210.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-01-21 08:00:00', 'resolved',    'transport', 'Vehicle Transport – Graz→Vienna', 'Enclosed car transport, no damage.',                                    'Herrengasse 1, 8010 Graz',           'Praterstr. 3, 1020 Vienna',          6, 7, 680.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-01-28 10:45:00', 'cancelled',   'moving',    'Office Relocation – Cancelled',   'Client cancelled 2 days before scheduled move.',                        'Schottenring 14, 1010 Vienna',       'Erdbergstr. 200, 1030 Vienna',       6, 7, 1100.00, 'EUR', 1, NULL, NULL, NULL),

-- ── February 2026 ────────────────────────────────────────────────────────
(REPLACE(UUID(),'-',''), '2026-02-03 09:00:00', 'resolved',    'moving',    'Student Move – Währing',          '1-room student flat, third floor, no elevator.',                        'Währinger Str. 60, 1090 Vienna',     'Alser Str. 30, 1080 Vienna',         6, 7, 1100.00, 'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-02-10 13:00:00', 'resolved',    'pickup',    'Electronics Store Pickup',        'Large TV and soundbar pickup from MediaMarkt.',                         'MediaMarkt Mariahilf, Vienna',       'Neubaugasse 5, 1070 Vienna',         6, 7, 190.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-02-17 10:00:00', 'resolved',    'transport', 'Vintage Car – Linz→Vienna',       'Soft-top sports car, handled with care.',                               'Hauptplatz 1, 4020 Linz',            'Gürtelstr. 10, 1100 Vienna',         6, 7, 560.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-02-20 15:00:00', 'cancelled',   'removal',   'Debris Removal – Permit Issue',   'Customer did not have proper disposal permit.',                         'Hernalser Hauptstr. 5, 1170 Vienna', 'Hernalser Hauptstr. 5, 1170 Vienna', 6, 7, 290.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-02-25 09:30:00', 'cancelled',   'moving',    'Piano Move – Access Too Narrow',  'Access too narrow, move could not be completed safely.',                'Pragerstr. 80, 1210 Vienna',         'Wagramer Str. 15, 1220 Vienna',      6, 7, 750.00,  'EUR', 1, NULL, NULL, NULL),

-- ── March 2026 ───────────────────────────────────────────────────────────
(REPLACE(UUID(),'-',''), '2026-03-02 08:30:00', 'resolved',    'moving',    'Family Home Move – Liesing',      '5-room house, full packing and unpacking service included.',            'Ketzergasse 200, 1230 Vienna',       'Breitenfurter Str. 99, 1230 Vienna', 6, 7, 1200.00, 'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-03-07 10:00:00', 'resolved',    'moving',    'Studio Flat Move – Neubau',       'Small move, student studio with minimal furniture.',                    'Siebensterng. 4, 1070 Vienna',       'Kircheng. 12, 1070 Vienna',          6, 7, 850.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-03-12 12:00:00', 'resolved',    'removal',   'Full Attic Clearance',            'Old furniture, boxes and electronics removed and disposed.',            'Döblinger Hauptstr. 30, 1190 Vienna','Döblinger Hauptstr. 30, 1190 Vienna',6, 7, 420.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-03-18 14:00:00', 'resolved',    'pickup',    'Appliance Pickup – Saturn',       'Washing machine and dryer pickup and delivery.',                        'Saturn Mariahilf, Vienna',           'Ottakringer Str. 20, 1160 Vienna',   6, 7, 160.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-03-24 09:00:00', 'resolved',    'transport', 'Motorbike – Salzburg→Vienna',     'Motorbike transported in enclosed trailer.',                            'Getreideg. 1, 5020 Salzburg',        'Ringstr. 5, 1010 Vienna',            6, 7, 730.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-03-27 11:00:00', 'cancelled',   'removal',   'Garage Clearance – Abandoned',    'Dispute over item classification, job abandoned.',                      'Simmeringer Hauptstr. 150, 1110 Vienna','Simmeringer Hauptstr. 150, 1110 Vienna',6,7,310.00,'EUR',1,NULL,NULL,NULL),

-- ── April 2026 ───────────────────────────────────────────────────────────
(REPLACE(UUID(),'-',''), '2026-04-02 09:00:00', 'resolved',    'moving',    'Couple Move – Donaustadt',        '2-room apartment, fragile items packed by crew.',                       'Erzherzog-Karl-Str. 40, 1220 Vienna','Aspernallee 2, 1220 Vienna',         6, 7, 980.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-04-08 10:30:00', 'resolved',    'transport', 'Classic Car – Vienna→Munich',     'Fully enclosed transport, overnight.',                                  'Erdbergstr. 200, 1030 Vienna',       'Leopoldstr. 1, 80802 München',       6, 7, 620.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-04-14 13:00:00', 'resolved',    'pickup',    'Garden Furniture Pickup – OBI',   'Large garden set from OBI, assembly at destination.',                   'OBI Wien Stadlau',                   'Breitenlee 50, 1220 Vienna',         6, 7, 200.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-04-19 09:30:00', 'resolved',    'removal',   'Post-Renovation Clearance',       'Building waste and leftover materials after renovation.',               'Taborstr. 22, 1020 Vienna',          'Taborstr. 22, 1020 Vienna',          6, 7, 380.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-04-22 11:00:00', 'cancelled',   'moving',    'International Move – Cancelled',  'Customer moved abroad, used another provider at last minute.',          'Hietzinger Hauptstr. 10, 1130 Vienna','Paris, France',                     6, 7, 1800.00, 'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-04-25 14:00:00', 'cancelled',   'pickup',    'Warehouse Pickup – Wrong Items',  'Wrong items prepared at warehouse, customer refused delivery.',         'Laxenburger Str. 100, 1100 Vienna',  'Mariahilfer Str. 50, 1060 Vienna',   6, 7, 150.00,  'EUR', 1, NULL, NULL, NULL),

-- ── May 2026 ─────────────────────────────────────────────────────────────
(REPLACE(UUID(),'-',''), '2026-05-03 09:00:00', 'resolved',    'moving',    'Penthouse Move – Innere Stadt',   'High-end move with artwork handling, elevator available.',              'Rotenturmstr. 1, 1010 Vienna',       'Schubertring 6, 1010 Vienna',        6, 7, 1050.00, 'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-05-09 10:00:00', 'resolved',    'removal',   'Office Equipment Clearance',      'Old desks, chairs and printers removed from office.',                  'Heiligenstädter Str. 31, 1190 Vienna','Heiligenstädter Str. 31, 1190 Vienna',6,7,290.00,'EUR',1,NULL,NULL,NULL),
(REPLACE(UUID(),'-',''), '2026-05-13 11:30:00', 'cancelled',   'moving',    'Move Cancelled – Same Morning',   'Customer cancelled same morning, no compensation agreed.',              'Gumpendorfer Str. 9, 1060 Vienna',   'Kaiserstr. 40, 1070 Vienna',         6, 7, 870.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-05-16 09:00:00', 'negotiation', 'transport', 'Truck Transport – In Progress',   'Company vehicle relocation, dates being finalised.',                   'Wienerberg, 1100 Vienna',            'Graz Hauptbahnhof, 8020 Graz',       6, 7, 540.00,  'EUR', 1, NULL, NULL, NULL),
(REPLACE(UUID(),'-',''), '2026-05-20 14:00:00', 'negotiation', 'pickup',    'Furniture Pickup – Confirming',   'IKEA order, delivery window being confirmed with customer.',           'IKEA Wien Nord',                     'Floridsdorfer Hauptstr. 1, 1210 Vienna',6,7,175.00,'EUR',1,NULL,NULL,NULL);

-- ── Verify ───────────────────────────────────────────────────────────────
SELECT MONTH(CreateDate) AS month, Status, Service, COUNT(*) AS cnt, SUM(Cost) AS revenue
FROM dev_Requests
WHERE ExecutorId = 7
GROUP BY MONTH(CreateDate), Status, Service
ORDER BY month, Status, Service;
