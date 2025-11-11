# Platforma "Social Bookmarking" – Cerințe proiect

Aplicația web se va realiza în **ASP.NET Core MVC** și **C#**, folosind **Entity Framework Core** pentru gestionarea datelor și **ASP.NET Identity** pentru autentificare și roluri.

## 1) Tipuri de utilizatori (0.5p)

Să existe cel puțin **3 tipuri de utilizatori**:
- Vizitator neînregistrat;
- Utilizator înregistrat;
- Administrator;

## 2) Afișarea și filtrarea bookmark-urilor (1.0p)

Orice utilizator (înregistrat sau nu) poate vizualiza bookmark-urile publice din platformă. Pe pagina principală vor fi afișate **cele mai recente** și **cele mai populare** bookmark-uri, cu posibilitatea filtrării (Recent / Populare).

Utilizatorul poate alege dacă dorește să vadă bookmark-urile recente sau pe cele populare.

### Explicație:
- Utilizatorii înregistrați pot acorda **voturi (like/unlike)** bookmarkurilor;
- Popularitatea se calculează în funcție de **numărul de voturi**;
- În funcție de numărul de voturi sau de data la care au fost create, se vor afișa cele mai recente sau cele mai votate bookmark-uri;
- Afișarea este **paginată**, astfel încât să se încarce un număr limitat de bookmark-uri per pagină;

## 3) Adăugarea și gestionarea bookmark-urilor (1.0p)

Utilizatorii înregistrați pot adăuga bookmark-uri care conțin:
- Titlu;
- Descriere;
- Conținut media (text, imagini, videoclipuri embed);

Autorul unui bookmark poate **edita** sau **șterge** propriile înregistrări.

## 4) Sistem de comentarii și votare (1.0p)

Utilizatorii înregistrați pot comenta și vota bookmark-urile existente. 

Fiecare utilizator poate **edita** sau **șterge** doar propriile comentarii.

## 5) Pagina personală cu categorii de bookmark-uri (1.5p)

Fiecare utilizator își poate salva bookmark-urile în **categorii personale**.

Categoriile pot fi:
- **private** (vizibile doar de autor)
- **publice** (vizibile tuturor)

Pe profilul unui utilizator se afișează toate categoriile **publice**, împreună cu bookmark-urile aferente.

Categoriile sunt create **dinamic de fiecare utilizator**. Fiecare categorie poate fi **adăugată, editată sau ștearsă** doar de cel care a creat-o.

## 6) Profilul utilizatorului (0.5p)

Profilul fiecărui membru va include:
- Nume și prenume;
- Descriere (secțiunea About);
- Imagine de profil;
- Bookmark-urile publice ale utilizatorului;

## 7) Motor de căutare (1.0p)

Platforma va include un **motor de căutare avansat** pentru bookmark-uri:
- Căutare după titlu, descriere și/sau categorie;
- Un utilizator poate cauta si dupa cuvinte cheie sau parti ale cuvintelor, deci platforma o să aibă suport pentru **căutare parțială** (ex: "sept" în loc de "septembrie");
- Afișarea rezultatelor va fi **paginată** și ordonată după relevanță (în funcție de numărul de voturi sau după data la care au fost create);

## 8) Componentă AI – Generare automată de tag-uri și categorii (1.0p)

La adăugarea unui bookmark, utilizatorul poate apăsa **"Sugerează cu AI"**.

Companionul virtual analizează **titlul** și **descrierea** și propune **2–3 tag-uri** și **2–3 categorii** potrivite (ex: "programare", "design", "vestimentație").

Utilizatorul poate **accepta, modifica sau ignora** sugestiile înainte de salvare.

### Criterii de verificare:
- Buton **"Sugerează cu AI"** disponibil în formularul de creare/editare;
- Se afișează **propuneri concrete (min. 2)** pe baza titlului și descrierii; pot fi bifate/ debifate;
- Salvare corectă a tag-urilor acceptate și (opțional) crearea categoriilor lipsă setate ca publice;

## 9) Administrare platformă (0.5p)

Administratorul are **acces complet** asupra conținutului și poate șterge bookmark-uri, comentarii sau categorii necorespunzătoare.

## 10) Calitatea proiectului și integrarea AI companion (1.0p)

Se punctează:
- Organizarea **corectă a aplicației MVC** (Models, Views, Controllers);
- **Validări de date** și **mesaje de eroare clare**;
- **Seed de date realist** (minim 3 utilizatori, 3 categorii și 5 bookmark-uri cu tag-uri asociate);
- Integrarea **corectă a companionului AI** și documentarea modului de funcționare;
- **README complet** – adică acel raport pe care trebuie să-l redactați;
