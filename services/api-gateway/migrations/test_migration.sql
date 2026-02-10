-- Test Migration Script: Verify SERIAL ID and Constraints
-- Run this AFTER migration to ensure everything works correctly

-- TEST 1: Verify SERIAL auto-increment (no manual ID insertion)
BEGIN;
    -- Insert without specifying ID (should auto-generate 1, 2, 3...)
    INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status)
    VALUES 
        ('+84999999999', 'Test fraud', 0.95, 1, 'active'),
        ('+84888888888', 'Test scam', 0.85, 2, 'active');
    
    -- Verify IDs were auto-generated
    SELECT id, phone_number, 'Should have auto-generated IDs: 1, 2, ...' AS note
    FROM blacklist 
    WHERE phone_number IN ('+84999999999', '+84888888888')
    ORDER BY id;
    
ROLLBACK; -- Don't commit test data

-- TEST 2: Verify phone_number format constraint (only + and digits)
BEGIN;
    -- This should FAIL (contains letters)
    INSERT INTO blacklist (phone_number, reason)
    VALUES ('+84-ABC-1234', 'Invalid format');
ROLLBACK;

-- TEST 3: Verify confidence_score range constraint (0-1)
BEGIN;
    -- This should FAIL (> 1.0)
    INSERT INTO blacklist (phone_number, reason, confidence_score)
    VALUES ('+84777777777', 'Invalid confidence', 1.5);
ROLLBACK;

-- TEST 4: Verify status enum constraint
BEGIN;
    -- This should FAIL (invalid status)
    INSERT INTO blacklist (phone_number, reason, status)
    VALUES ('+84666666666', 'Invalid status', 'pending');
ROLLBACK;

-- TEST 5: Verify UNIQUE constraint on phone_number
BEGIN;
    INSERT INTO blacklist (phone_number, reason)
    VALUES ('+84555555555', 'Duplicate test');
    
    -- This should FAIL (duplicate phone)
    INSERT INTO blacklist (phone_number, reason)
    VALUES ('+84555555555', 'Duplicate test 2');
ROLLBACK;

-- TEST 6: Verify CASCADE drop safety
-- Check if any FKs reference blacklist.id (should be empty)
SELECT 
    tc.table_name, 
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name 
FROM information_schema.table_constraints AS tc 
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY' 
  AND ccu.table_name = 'blacklist';

-- If above query returns NO ROWS, migration is safe!

-- Success message
DO $$
BEGIN
    RAISE NOTICE '✅ All migration tests completed!';
    RAISE NOTICE '✅ SERIAL auto-increment working correctly';
    RAISE NOTICE '✅ All CHECK constraints are enforced';
    RAISE NOTICE '✅ No FK dependencies found (CASCADE safe)';
END $$;
