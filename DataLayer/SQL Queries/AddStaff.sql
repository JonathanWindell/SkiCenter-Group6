DECLARE @Email    nvarchar(200) = 'SystemAdmin@Test.se';
DECLARE @Password varchar(200)  = 'testpassword';  -- at least 10 characters, no å/ä/ö

INSERT INTO StaffMembers (FirstName, LastName, Email, Password, Role)
VALUES (
    'System',
    'Admin',
    @Email,
    LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', @Password), 2)),
    3  -- 3 = SystemAdmin (SkiShop = 0, MarketingManager = 1, BookingAdmin = 2)
);

SELECT StaffID, Email, Role FROM StaffMembers WHERE Email = @Email;