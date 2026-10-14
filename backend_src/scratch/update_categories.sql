UPDATE Categories SET Name = 'Burger', IconClass = 'bi bi-egg-fried', ColorHex = '#f59e0b' WHERE Id = 2;
UPDATE Categories SET Name = 'Sandwich', IconClass = 'bi bi-egg-fried', ColorHex = '#f59e0b' WHERE Id = 3;
UPDATE Categories SET Name = 'Fries', IconClass = 'bi bi-egg-fried', ColorHex = '#f59e0b' WHERE Id = 4;
UPDATE Categories SET Name = 'Add ons', IconClass = 'bi bi-plus-circle', ColorHex = '#f59e0b' WHERE Id = 5;
UPDATE Categories SET Name = 'Wraps', IconClass = 'bi bi-egg-fried', ColorHex = '#f59e0b' WHERE Id = 6;
INSERT INTO Categories (Name, IconClass, ColorHex, IsActive, CreatedAt) VALUES ('Premium Menu', 'bi bi-star-fill', '#f59e0b', 1, GETUTCDATE());

UPDATE Products SET CategoryId = (SELECT Id FROM Categories WHERE Name = 'Burger') WHERE Name LIKE '%Burger%';
UPDATE Products SET CategoryId = (SELECT Id FROM Categories WHERE Name = 'Sandwich') WHERE Name LIKE '%Sandwich%';
UPDATE Products SET CategoryId = (SELECT Id FROM Categories WHERE Name = 'Fries') WHERE Name LIKE '%Fries%';
UPDATE Products SET CategoryId = (SELECT Id FROM Categories WHERE Name = 'Wraps') WHERE Name LIKE '%Wrap%';
UPDATE Products SET CategoryId = (SELECT Id FROM Categories WHERE Name = 'Beverages') WHERE Name IN ('Pepsi / Coke', 'Sprite / 7UP', 'Fanta / Mirinda', 'Mountain Dew', 'Sting', 'Mineral Water', 'Green Tea', 'Cappuccino');
UPDATE Products SET CategoryId = (SELECT Id FROM Categories WHERE Name = 'Add ons') WHERE Name IN ('Egg', 'Cheese', 'Extra Cheese', 'Wings (5 pcs)', 'Chicken Strips');
UPDATE Products SET CategoryId = (SELECT Id FROM Categories WHERE Name = 'Premium Menu') WHERE Name LIKE '%Broast%' OR Name LIKE '%Piece%' OR Name LIKE '%Breadline Special%';
