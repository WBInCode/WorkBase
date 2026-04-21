-- Audit entries immutability enforcement
-- Run this after EF Core migrations to lock down the audit_entries table.
-- Only INSERT is permitted; UPDATE and DELETE are revoked for all non-superuser roles.

DO $$
DECLARE
    app_role TEXT;
BEGIN
    -- Revoke from PUBLIC (covers all roles by default)
    REVOKE UPDATE, DELETE ON public.audit_entries FROM PUBLIC;

    -- Also create a rule that prevents DELETE/UPDATE even for the table owner
    IF NOT EXISTS (SELECT 1 FROM pg_rules WHERE rulename = 'audit_entries_no_update') THEN
        CREATE RULE audit_entries_no_update AS ON UPDATE TO public.audit_entries DO INSTEAD NOTHING;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_rules WHERE rulename = 'audit_entries_no_delete') THEN
        CREATE RULE audit_entries_no_delete AS ON DELETE TO public.audit_entries DO INSTEAD NOTHING;
    END IF;

    RAISE NOTICE 'audit_entries immutability rules applied successfully.';
END $$;
