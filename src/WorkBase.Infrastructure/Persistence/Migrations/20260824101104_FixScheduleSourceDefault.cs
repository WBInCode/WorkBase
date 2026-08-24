using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkBase.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Domyslna wartosc kolumny <c>time_schedules.source</c> w bazie byla 0 (OrgUnit), a model
    /// od dawna deklaruje 1 (Individual) — migracji nikt nie wygenerowal, bo strazniki EF byly
    /// wyciszone. Skutek nie byl kosmetyczny: konfiguracja uzywa
    /// <c>HasSentinel(ScheduleSource.Individual)</c>, wiec EF POMIJA te kolumne w INSERT wlasnie
    /// wtedy, gdy grafik jest indywidualny — i baza wstawiala 0. Kazdy grafik ustawiony recznie
    /// pracownikowi zapisywal sie jako grafik jednostki.
    ///
    /// Dwie konsekwencje w dzialajacym systemie:
    ///  - cotygodniowe zadanie OrgUnitScheduleRollingGenerationJob kasuje wszystkie wpisy
    ///    o Source == OrgUnit i odtwarza je z szablonu jednostki. Grafiki indywidualne, ktore
    ///    mialo omijac, wygladaly dla niego jak wlasne — czyli byly do skasowania.
    ///  - ClearSchedulesHandler bez IncludeOrgUnitGenerated kasuje tylko Source != OrgUnit,
    ///    wiec "wyczysc grafik" nie kasowal niczego.
    /// </summary>
    public partial class FixScheduleSourceDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "source",
                table: "time_schedules",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            // Naprawa juz zapisanych wierszy. Generator zawsze ustawia source ORAZ
            // org_unit_schedule_id razem (OrgUnitScheduleGeneratorService), wiec wiersz
            // z source = OrgUnit i pustym powiazaniem nie mogl powstac z grafiku jednostki —
            // to grafik indywidualny zapisany bledna wartoscia domyslna.
            migrationBuilder.Sql(@"
                UPDATE time_schedules
                SET source = 1
                WHERE source = 0 AND org_unit_schedule_id IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "source",
                table: "time_schedules",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);
        }
    }
}
