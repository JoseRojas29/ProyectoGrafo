using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using ArbolGenealogico.Modelos;

namespace ArbolGenealogico.Tests
{
    [TestClass]
    public class ArbolGenealogicoTests
    {
        // ======================================
        // 1. PRUEBA: Asignar padre correctamente
        // ======================================
        [TestMethod]
        public void TestAsignarPadre_AsignaCorrectamente()
        {
            var padre = new MiembroFamilia("Carlos", "1", new DateTime(1980, 1, 1), true);
            var hijo = new MiembroFamilia("Pedro", "2", new DateTime(2010, 1, 1), true);

            hijo.AsignarPadre(padre);

            Assert.AreEqual(padre, hijo.Padre);
            Assert.IsTrue(padre.Hijos.Contains(hijo));
        }


        // ======================================
        // 2. PRUEBA: Asignar madre correctamente
        // ======================================
        [TestMethod]
        public void TestAsignarMadre_AsignaCorrectamente()
        {
            var madre = new MiembroFamilia("Ana", "3", new DateTime(1982, 1, 1), true);
            var hijo = new MiembroFamilia("Luis", "4", new DateTime(2010, 1, 1), true);

            hijo.AsignarMadre(madre);

            Assert.AreEqual(madre, hijo.Madre);
            Assert.IsTrue(madre.Hijos.Contains(hijo));
        }


        // ======================================
        // 3. PRUEBA: Impedir dos padres distintos
        // ======================================
        [TestMethod]
        public void TestAsignarPadre_NoPermiteDosPadres()
        {
            var padre1 = new MiembroFamilia("Juan", "10", new DateTime(1970, 1, 1), true);
            var padre2 = new MiembroFamilia("Luis", "11", new DateTime(1975, 1, 1), true);
            var hijo = new MiembroFamilia("Andrés", "12", new DateTime(2000, 1, 1), true);

            hijo.AsignarPadre(padre1);

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                hijo.AsignarPadre(padre2);
            });
        }


        // ======================================
        // 4. PRUEBA: Asignar pareja
        // ======================================
        [TestMethod]
        public void TestAsignarPareja_Bidireccional()
        {
            var a = new MiembroFamilia("Carlos", "20", DateTime.Now.AddYears(-40), true);
            var b = new MiembroFamilia("María", "21", DateTime.Now.AddYears(-38), true);

            a.AsignarPareja(b);

            Assert.AreEqual(b, a.Pareja);
            Assert.AreEqual(a, b.Pareja);
        }


        // ======================================
        // 5. PRUEBA: Obtener hermanos por padre y madre
        // ======================================
        [TestMethod]
        public void TestObtenerHermanos_Combinados()
        {
            var padre = new MiembroFamilia("Luis", "30", DateTime.Now.AddYears(-50), true);
            var madre = new MiembroFamilia("Ana", "31", DateTime.Now.AddYears(-48), true);

            var hijo1 = new MiembroFamilia("Carlos", "32", DateTime.Now.AddYears(-20), true);
            var hijo2 = new MiembroFamilia("María", "33", DateTime.Now.AddYears(-18), true);

            hijo1.AsignarPadre(padre);
            hijo1.AsignarMadre(madre);
            hijo2.AsignarPadre(padre);
            hijo2.AsignarMadre(madre);

            var hermanos = hijo1.ObtenerHermanos();

            Assert.AreEqual(1, hermanos.Count);
            Assert.AreEqual(hijo2, hermanos[0]);
        }


        // ======================================
        // 6. PRUEBA: Agregar primer nodo del árbol
        // ======================================
        [TestMethod]
        public void TestAgregarPrimerNodo()
        {
            var arbol = new ArbolGenealogico();
            var raiz = new MiembroFamilia("Elena", "40", DateTime.Now.AddYears(-70), false);

            bool agregado = arbol.AgregarPrimerNodo(raiz);

            Assert.IsTrue(agregado);
            Assert.AreEqual(raiz, arbol.Raiz);
        }


        // ======================================
        // 7. PRUEBA: No permitir reemplazar raíz existente
        // ======================================
        [TestMethod]
        public void TestAgregarPrimerNodo_NoReemplazaExistente()
        {
            var arbol = new ArbolGenealogico();
            var raiz1 = new MiembroFamilia("Roberto", "41", DateTime.Now.AddYears(-72), false);
            var raiz2 = new MiembroFamilia("Clara", "42", DateTime.Now.AddYears(-75), false);

            arbol.AgregarPrimerNodo(raiz1);
            bool result = arbol.AgregarPrimerNodo(raiz2);

            Assert.IsFalse(result);
            Assert.AreEqual(raiz1, arbol.Raiz);
        }


        // ======================================
        // 8. PRUEBA: Detectar parejas automáticamente (mismos hijos)
        // ======================================
        [TestMethod]
        public void TestDetectarParejasAutomaticas()
        {
            var madre = new MiembroFamilia("Ana", "50", DateTime.Now.AddYears(-40), true);
            var padre = new MiembroFamilia("Luis", "51", DateTime.Now.AddYears(-42), true);
            var hijo = new MiembroFamilia("Tomás", "52", DateTime.Now.AddYears(-10), true);

            hijo.AsignarPadre(padre);
            hijo.AsignarMadre(madre);

            var miembros = new List<MiembroFamilia> { padre, madre, hijo };
            var arbol = new ArbolGenealogico();

            arbol.DetectarParejas(miembros);

            Assert.AreEqual(madre, padre.Pareja);
            Assert.AreEqual(padre, madre.Pareja);
        }


        // ======================================
        // 9. PRUEBA: Generar grafo (Padre/Madre / hijo)
        // ======================================
        [TestMethod]
        public void TestGenerarGrafo_PadreMadreHijo()
        {
            var padre = new MiembroFamilia("Luis", "60", DateTime.Now.AddYears(-40), true);
            var madre = new MiembroFamilia("Ana", "61", DateTime.Now.AddYears(-39), true);
            var hijo = new MiembroFamilia("Carlos", "62", DateTime.Now.AddYears(-12), true);

            hijo.AsignarPadre(padre);
            hijo.AsignarMadre(madre);

            var miembros = new List<MiembroFamilia> { padre, madre, hijo };
            var arbol = new ArbolGenealogico();

            var grafo = arbol.GenerarGrafo(miembros);

            // Buscar relaciones
            Assert.IsTrue(grafo.Exists(a => a.A == padre && a.B == hijo && a.Peso == 2));
            Assert.IsTrue(grafo.Exists(a => a.A == madre && a.B == hijo && a.Peso == 2));
        }


        // ======================================
        // 10. PRUEBA: Hermanos en el grafo
        // ======================================
        [TestMethod]
        public void TestGenerarGrafo_Hermanos()
        {
            var padre = new MiembroFamilia("Juan", "70", DateTime.Now.AddYears(-50), true);
            var hijo1 = new MiembroFamilia("Pedro", "71", DateTime.Now.AddYears(-20), true);
            var hijo2 = new MiembroFamilia("María", "72", DateTime.Now.AddYears(-18), true);

            hijo1.AsignarPadre(padre);
            hijo2.AsignarPadre(padre);

            var miembros = new List<MiembroFamilia> { padre, hijo1, hijo2 };
            var arbol = new ArbolGenealogico();

            var grafo = arbol.GenerarGrafo(miembros);

            Assert.IsTrue(grafo.Exists(a =>
                a.Peso == 3 &&
                ((a.A == hijo1 && a.B == hijo2) || (a.A == hijo2 && a.B == hijo1))));
        }
    }
}
