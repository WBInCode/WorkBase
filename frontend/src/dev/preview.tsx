// Dev-only design preview — renders the redesigned shell with mocked data,
// no Keycloak / API required. Open http://localhost:5173/dev-preview.html
// NOT included in the production build (only main index.html entry is built).
import { StrictMode, useState } from 'react';
import { createRoot } from 'react-dom/client';
import '../theme/workbase.css';
import {
  Briefcase, LayoutDashboard, FolderTree, Users, FileUp, CalendarDays, UsersRound,
  CalendarClock, Wallet, Palmtree, ClipboardCheck, CalendarRange, ListTodo, ClipboardList,
  FileArchive, FolderOpen, Menu, Clock, Bell, ChevronDown, LogOut, Plus,
  AlertTriangle, Play,
} from 'lucide-react';
import { colors } from '../theme/tokens';

function Rail() {
  const sections: { title: string | null; items: { icon: typeof Briefcase; label: string; active?: boolean }[] }[] = [
    { title: null, items: [
      { icon: Briefcase, label: 'Mój dzień', active: true },
      { icon: LayoutDashboard, label: 'Dashboard' },
    ]},
    { title: 'Organizacja', items: [
      { icon: FolderTree, label: 'Struktura' },
      { icon: Users, label: 'Pracownicy' },
      { icon: FileUp, label: 'Import CSV' },
    ]},
    { title: 'Czas pracy', items: [
      { icon: CalendarDays, label: 'Karta czasu' },
      { icon: UsersRound, label: 'Raport zespołu' },
      { icon: CalendarClock, label: 'Grafik pracy' },
      { icon: Wallet, label: 'Wynagrodzenia' },
    ]},
    { title: 'Urlopy', items: [
      { icon: Palmtree, label: 'Wnioski' },
      { icon: ClipboardCheck, label: 'Akceptacje' },
      { icon: CalendarRange, label: 'Kalendarz' },
    ]},
    { title: 'Zadania', items: [
      { icon: ListTodo, label: 'Wszystkie' },
      { icon: ClipboardList, label: 'Moje zadania' },
    ]},
    { title: 'Dokumenty', items: [
      { icon: FileArchive, label: 'Pliki' },
      { icon: FolderOpen, label: 'Kategorie' },
    ]},
  ];
  return (
    <aside className="wb-rail" style={{ width: 248, margin: '12px 0 12px 12px' }}>
      <div className="wb-rail-head">
        <div style={{
          width: 34, height: 34, borderRadius: 10,
          background: 'linear-gradient(135deg, #3d6df2, #2b55d4)',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          fontSize: 16, fontWeight: 800, color: '#fff', flexShrink: 0,
          boxShadow: '0 6px 14px -4px rgba(61,109,242,0.5)',
        }}>W</div>
        <div style={{ minWidth: 0 }}>
          <div style={{ fontSize: 15.5, fontWeight: 800, color: 'var(--wb-ink)', letterSpacing: '-0.02em' }}>WorkBase</div>
          <div style={{ fontSize: 10, fontWeight: 600, color: '#9aa3bc', letterSpacing: '0.06em', textTransform: 'uppercase' }}>Platforma HR</div>
        </div>
      </div>
      <nav className="wb-nav-scroll" style={{ flex: 1, overflowY: 'auto', padding: '2px 10px 12px' }}>
        {sections.map((s, i) => (
          <div key={i} style={{ marginBottom: 2 }}>
            {s.title && (
              <button type="button" className="wb-nav-sec">
                {s.title}
                <ChevronDown size={12} className="wb-sec-chev" />
              </button>
            )}
            {s.items.map((it) => {
              const Icon = it.icon;
              return (
                <a key={it.label} href="#" onClick={(e) => e.preventDefault()} className={`wb-nav-item${it.active ? ' is-active' : ''}`}>
                  <span className="wb-nav-ico"><Icon size={15} /></span>
                  {it.label}
                </a>
              );
            })}
          </div>
        ))}
      </nav>
      <div className="wb-rail-foot">
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 10 }}>
          <div style={{
            width: 34, height: 34, borderRadius: '50%',
            background: 'linear-gradient(135deg, #3d6df2, #2b55d4)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            fontSize: 13, fontWeight: 700, color: '#fff',
            boxShadow: '0 4px 10px -3px rgba(61,109,242,0.45)',
          }}>J</div>
          <div style={{ minWidth: 0 }}>
            <div style={{ fontSize: 13, fontWeight: 700, color: 'var(--wb-ink)' }}>Jan Kowalski</div>
            <div style={{ fontSize: 11, color: '#9aa3bc' }}>jan@firma.pl</div>
          </div>
        </div>
        <button style={{
          display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6, width: '100%',
          padding: '8px 10px', fontSize: 12, fontWeight: 600, fontFamily: 'inherit',
          color: '#4c5570', backgroundColor: '#fff', border: '1px solid var(--wb-line)',
          borderRadius: 999, cursor: 'pointer', boxShadow: '0 1px 2px rgba(20,25,43,0.05)',
        }}>
          <LogOut size={13} /> Wyloguj
        </button>
      </div>
    </aside>
  );
}

function Topbar() {
  return (
    <header className="wb-topbar">
      <button className="wb-icon-btn" aria-label="Menu"><Menu size={18} /></button>
      <div className="wb-topbar-title">Mój dzień</div>
      <div style={{ flex: 1 }} />
      <div className="wb-clock-chip"><Clock size={13} /><span>09:41:23</span></div>
      <button className="wb-icon-btn" aria-label="Powiadomienia" style={{ position: 'relative' }}>
        <Bell size={18} />
        <span style={{
          position: 'absolute', top: 2, right: 2, background: '#ef4444', color: '#fff',
          fontSize: 9, fontWeight: 700, borderRadius: 999, minWidth: 14, height: 14,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
        }}>3</span>
      </button>
      <button style={{
        display: 'inline-flex', alignItems: 'center', gap: 8, padding: '8px 16px',
        fontSize: 13, fontWeight: 700, fontFamily: 'inherit', color: '#fff',
        background: 'linear-gradient(135deg, #22c55e, #16a34a)', border: 'none',
        borderRadius: 999, cursor: 'pointer', boxShadow: '0 6px 14px -4px rgba(34,197,94,0.5)',
      }}>
        <Play size={14} /> Rozpocznij pracę
      </button>
    </header>
  );
}

function Card({ title, icon, children, accent }: { title: string; icon?: React.ReactNode; children: React.ReactNode; accent?: string }) {
  return (
    <div style={{
      backgroundColor: '#fff', borderRadius: '16px', border: `1px solid ${colors.gray[200]}`,
      padding: '20px',
      boxShadow: '0 1px 2px rgba(20,25,43,0.04), 0 10px 30px -12px rgba(20,25,43,0.10), inset 0 1px 0 var(--wb-card-hl, rgba(255,255,255,0.9))',
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 14 }}>
        {icon}
        <span style={{ fontSize: 14, fontWeight: 700, color: colors.gray[700] }}>{title}</span>
        {accent && (
          <span style={{
            marginLeft: 'auto', padding: '2px 10px', borderRadius: 999, fontSize: 12,
            fontWeight: 700, backgroundColor: colors.warning[100], color: colors.warning[800],
          }}>{accent}</span>
        )}
      </div>
      {children}
    </div>
  );
}

function Preview() {
  const [tab, setTab] = useState<'cards' | 'form'>('cards');
  return (
    <div className="wb-shell">
      <Rail />
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden', minWidth: 0 }}>
        <Topbar />
        <main className="wb-page-enter" style={{ flex: 1, overflow: 'auto' }}>
          <div style={{ padding: 24, maxWidth: 1000 }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 24 }}>
              <div>
                <h1 style={{ margin: 0, fontSize: 22, fontWeight: 800, color: colors.gray[900], letterSpacing: '-0.02em' }}>
                  Dzień dobry, Jan
                </h1>
                <p style={{ margin: '4px 0 0', fontSize: 14, color: colors.gray[500] }}>
                  piątek, 10 lipca 2026
                </p>
              </div>
              <div style={{ display: 'flex', gap: 8 }}>
                <button onClick={() => setTab('cards')} style={{
                  padding: '8px 18px', fontSize: 13, fontWeight: 700, fontFamily: 'inherit',
                  color: tab === 'cards' ? '#fff' : colors.gray[600],
                  background: tab === 'cards' ? colors.primary[500] : '#fff',
                  border: `1px solid ${tab === 'cards' ? colors.primary[500] : colors.gray[300]}`,
                  borderRadius: 999, cursor: 'pointer',
                  boxShadow: tab === 'cards' ? '0 6px 14px -4px rgba(61,109,242,0.45)' : 'none',
                }}>Widgety</button>
                <button onClick={() => setTab('form')} style={{
                  padding: '8px 18px', fontSize: 13, fontWeight: 700, fontFamily: 'inherit',
                  color: tab === 'form' ? '#fff' : colors.gray[600],
                  background: tab === 'form' ? colors.primary[500] : '#fff',
                  border: `1px solid ${tab === 'form' ? colors.primary[500] : colors.gray[300]}`,
                  borderRadius: 999, cursor: 'pointer',
                  boxShadow: tab === 'form' ? '0 6px 14px -4px rgba(61,109,242,0.45)' : 'none',
                }}>Formularz</button>
              </div>
            </div>

            {tab === 'cards' ? (
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>
                <Card title="Mój dzień" icon={<Clock size={18} color={colors.primary[500]} />}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 16 }}>
                    <div style={{
                      width: 48, height: 48, borderRadius: 14, backgroundColor: colors.success[100],
                      display: 'flex', alignItems: 'center', justifyContent: 'center',
                    }}>
                      <Clock size={24} color={colors.success[600]} />
                    </div>
                    <div>
                      <div style={{ fontSize: 18, fontWeight: 800, color: colors.success[600] }}>W pracy</div>
                      <div style={{ fontSize: 12, color: colors.gray[400] }}>od 08:02</div>
                    </div>
                  </div>
                  <div style={{ display: 'flex', gap: 24 }}>
                    <div>
                      <div style={{ fontSize: 11, color: colors.gray[400], textTransform: 'uppercase', letterSpacing: '0.5px', fontWeight: 700 }}>Przepracowano</div>
                      <div className="wb-tnum" style={{ fontSize: 20, fontWeight: 700, color: colors.gray[900] }}>01:39:12</div>
                    </div>
                    <div>
                      <div style={{ fontSize: 11, color: colors.gray[400], textTransform: 'uppercase', letterSpacing: '0.5px', fontWeight: 700 }}>Przerwy</div>
                      <div className="wb-tnum" style={{ fontSize: 20, fontWeight: 700, color: colors.gray[500] }}>00:15:00</div>
                    </div>
                  </div>
                </Card>

                <Card title="Oczekujące akceptacje" icon={<ClipboardCheck size={18} color={colors.warning[500]} />} accent="2">
                  {[['Wniosek urlopowy', 'Anna Nowak · 8–12 lip'], ['Korekta czasu', 'Piotr Wiśniewski · wczoraj']].map(([t, s]) => (
                    <div key={t} style={{
                      display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                      padding: '10px 12px', borderRadius: 12, backgroundColor: colors.gray[50], marginBottom: 6, cursor: 'pointer',
                    }}>
                      <div>
                        <div style={{ fontSize: 13, fontWeight: 600, color: colors.gray[900] }}>{t}</div>
                        <div style={{ fontSize: 12, color: colors.gray[400] }}>{s}</div>
                      </div>
                      <ChevronDown size={14} style={{ transform: 'rotate(-90deg)' }} color={colors.gray[400]} />
                    </div>
                  ))}
                </Card>

                <Card title="Moje zadania" icon={<ListTodo size={18} color="#7c3aed" />}>
                  <div style={{
                    display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                    padding: '10px 12px', borderRadius: 12, backgroundColor: colors.gray[50],
                    borderLeft: `3px solid ${colors.danger[600]}`, marginBottom: 6,
                  }}>
                    <div>
                      <div style={{ fontSize: 14, fontWeight: 600, color: colors.gray[900] }}>Raport kwartalny Q2</div>
                      <div style={{ fontSize: 12, color: colors.gray[400], marginTop: 2 }}>
                        <span style={{ padding: '1px 8px', borderRadius: 999, fontSize: 11, fontWeight: 600, background: colors.primary[100], color: colors.primary[700] }}>W toku</span>
                      </div>
                    </div>
                    <span style={{ fontSize: 12, color: colors.danger[600], fontWeight: 700 }}>
                      <AlertTriangle size={12} style={{ verticalAlign: 'middle', marginRight: 3 }} />
                      wczoraj
                    </span>
                  </div>
                  <div style={{
                    display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                    padding: '10px 12px', borderRadius: 12, backgroundColor: colors.gray[50],
                  }}>
                    <div style={{ fontSize: 14, fontWeight: 600, color: colors.gray[900] }}>Przegląd umów</div>
                    <span style={{ fontSize: 12, color: colors.gray[400] }}>15.07.2026</span>
                  </div>
                </Card>

                <Card title="Moje urlopy" icon={<Palmtree size={18} color={colors.emerald[600]} />}>
                  <div style={{
                    display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                    padding: '10px 12px', borderRadius: 12, backgroundColor: colors.gray[50],
                  }}>
                    <div>
                      <div style={{ fontSize: 13, fontWeight: 600, color: colors.gray[900] }}>Urlop wypoczynkowy</div>
                      <div style={{ fontSize: 12, color: colors.gray[400], marginTop: 2 }}>03.08 – 14.08 (10 dni)</div>
                    </div>
                    <span style={{ padding: '2px 10px', borderRadius: 999, fontSize: 11, fontWeight: 700, backgroundColor: '#d1fae5', color: '#065f46' }}>
                      Zaakceptowany
                    </span>
                  </div>
                </Card>
              </div>
            ) : (
              <div style={{
                backgroundColor: '#fff', borderRadius: 20, padding: 24, maxWidth: 520,
                border: `1px solid ${colors.gray[200]}`,
                boxShadow: '0 24px 64px -12px rgba(20,25,43,0.28), 0 0 0 1px rgba(20,25,43,0.04)',
              }}>
                <h2 style={{ margin: '0 0 18px', fontSize: 17, fontWeight: 800, color: colors.gray[900] }}>Nowa rola</h2>
                <label style={{ display: 'block', fontSize: 12.5, fontWeight: 700, color: colors.gray[600], marginBottom: 6 }}>Nazwa</label>
                <input placeholder="np. Kierownik zmiany" style={{
                  width: '100%', boxSizing: 'border-box', padding: '10px 14px', fontSize: 14, fontFamily: 'inherit',
                  border: `1px solid ${colors.gray[300]}`, borderRadius: 10, marginBottom: 14, outline: 'none',
                }} />
                <label style={{ display: 'block', fontSize: 12.5, fontWeight: 700, color: colors.gray[600], marginBottom: 6 }}>Opis</label>
                <textarea rows={3} placeholder="Czym zajmuje się ta rola?" style={{
                  width: '100%', boxSizing: 'border-box', padding: '10px 14px', fontSize: 14, fontFamily: 'inherit',
                  border: `1px solid ${colors.gray[300]}`, borderRadius: 10, marginBottom: 20, outline: 'none', resize: 'vertical',
                }} />
                <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
                  <button style={{
                    padding: '9px 18px', fontSize: 13, fontWeight: 600, fontFamily: 'inherit',
                    color: colors.gray[700], backgroundColor: '#fff', border: `1px solid ${colors.gray[300]}`,
                    borderRadius: 999, cursor: 'pointer',
                  }}>Anuluj</button>
                  <button style={{
                    display: 'inline-flex', alignItems: 'center', gap: 6, padding: '9px 20px',
                    fontSize: 13, fontWeight: 700, fontFamily: 'inherit', color: '#fff',
                    backgroundColor: colors.primary[600], border: 'none', borderRadius: 999,
                    cursor: 'pointer', boxShadow: '0 6px 14px -4px rgba(61,109,242,0.45)',
                  }}>
                    <Plus size={15} /> Utwórz rolę
                  </button>
                </div>
              </div>
            )}
          </div>
        </main>
      </div>
    </div>
  );
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Preview />
  </StrictMode>,
);
