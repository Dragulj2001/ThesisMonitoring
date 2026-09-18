# Monitoring Objekata (Thesis Monitoring)

Ovaj projekat predstavlja veb-bazirani informacioni sistem za automatizovan monitoring mostovskih konstrukcija. Sistem omogućava centralizovan nadzor, automatsku obradu podataka i detekciju strukturnih deformacija u realnom vremenu.

## Glavne funkcije
- **Automatski monitoring:** Kontinuirano praćenje stanja prizmi pomoću robotske totalne stanice.
- **Virtuelni senzor:** Matematički model za proračun relativnog ulegnuća ($dZ_{dif}$) koji eliminiše šum globalnog sleganja tla.
- **Automatizovano alarmiranje:** Sistem šalje Email obaveštenja čim se detektuje prekoračenje definisanih granica stabilnosti.
- **Administrativni Dashboard:** Upravljanje mostovima, prizmama i podešavanje alarmnih pragova.
- **Excel Import/Export:** Brza obrada sirovih terenskih podataka.

## Tehnologije
- **Backend:** ASP.NET Core (C#)
- **Baza:** PostgreSQL
- **Frontend:** Bootstrap 5, Chart.js, SVG vizualizacija
- **Deploy:** Render.com (Docker)

## Kako pokrenuti projekat
1. Klonirajte repozitorijum.
2. Podesite Connection String u `appsettings.json`.
3. Pokrenite migracije baze: `dotnet ef database update`.
4. Pokrenite aplikaciju: `dotnet watch`.

## Dokumentacija
Diplomski rad "Veb-bazirani informacioni sistem za automatizovanu obradu i vizuelizaciju podataka geodetskog monitoringa" (2026).