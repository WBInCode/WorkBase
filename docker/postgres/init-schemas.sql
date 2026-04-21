-- Auto-create schemas needed by services sharing the WorkBase database
CREATE SCHEMA IF NOT EXISTS keycloak;

-- Audit entries immutability: revoke UPDATE and DELETE on audit_entries
-- This runs after EF Core creates the table (idempotent via DO block)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'audit_entries' AND table_schema = 'public') THEN
        REVOKE UPDATE, DELETE ON public.audit_entries FROM PUBLIC;
        RAISE NOTICE 'REVOKE UPDATE/DELETE on audit_entries applied.';
    ELSE
        RAISE NOTICE 'audit_entries table not yet created — REVOKE will apply on next init.';
    END IF;
END $$;
