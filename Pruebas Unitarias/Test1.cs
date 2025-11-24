using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using ArbolGenealogicoWPF;
using ArbolGenealogicoWPF.Modelos;

namespace Pruebas_Unitarias
{
    [TestClass]
    public class ArbolGenealogicoServiceTests
    {
        // ================================================================
        // 0. NO PERMITIR NODO NULO
        // ================================================================
        [TestMethod]
        public void TestAgregarNodo_NuevoNulo_NoDebeAgregar()
        {
            var seleccionado = new MiembroFamilia("Carlos", "1", DateTime.Now.AddYears(-40), true);

            bool resultado = ArbolGenealogicoService.AgregarNodo(null, seleccionado, null, 0);

            Assert.IsFalse(resultado);
        }


        // ================================================================
        // 1. AGREGAR PADRE
        // ================================================================
        [TestMethod]
        public void TestAgregarNodo_Padre()
        {
            var hijo = new MiembroFamilia("Pedro", "2", DateTime.Now.AddYears(-10), true);
            var padre = new MiembroFamilia("Luis", "3", DateTime.Now.AddYears(-40), true);

            bool resultado = ArbolGenealogicoService.AgregarNodo(null, hijo, padre, 0);

            Assert.IsTrue(resultado);
            Assert.AreEqual(padre, hijo.Padre);
            Assert.IsTrue(padre.Hijos.Contains(hijo));
        }


        // ================================================================
        // 2. AGREGAR MADRE
        // ================================================================
        [TestMethod]
        public void TestAgregarNodo_Madre()
        {
            var hijo = new MiembroFamilia("Pedro", "2", DateTime.Now.AddYears(-10), true);
            var madre = new MiembroFamilia("Ana", "4", DateTime.Now.AddYears(-38), true);

            bool resultado = ArbolGenealogicoService.AgregarNodo(null, hijo, madre, 1);

            Assert.IsTrue(resultado);
            Assert.AreEqual(madre, hijo.Madre);
            Assert.IsTrue(madre.Hijos.Contains(hijo));
        }


        // ================================================================
        // 3. AGREGAR HIJO DESDE PADRE
        // ================================================================
        [TestMethod]
        public void TestAgregarNodo_HijoComoPadre()
        {
            var padre = new MiembroFamilia("Luis", "3", DateTime.Now.AddYears(-40), true);
            var hijo = new MiembroFamilia("Pedro", "2", DateTime.Now.AddYears(-10), true);

            bool resultado = ArbolGenealogicoService.AgregarNodo(null, padre, hijo, 2);

            Assert.IsTrue(resultado);
            Assert.AreEqual(padre, hijo.Padre);
            Assert.IsTrue(padre.Hijos.Contains(hijo));
        }

        // ================================================================
        // 4. AGREGAR HIJO DESDE MADRE
        // ================================================================
        [TestMethod]
        public void TestAgregarNodo_HijoComoMadre()
        {
            var madre = new MiembroFamilia("Ana", "4", DateTime.Now.AddYears(-38), true);
            var hijo = new MiembroFamilia("Pedro", "2", DateTime.Now.AddYears(-10), true);

            bool resultado = ArbolGenealogicoService.AgregarNodo(null, madre, hijo, 3);

            Assert.IsTrue(resultado);
            Assert.AreEqual(madre, hijo.Madre);
            Assert.IsTrue(madre.Hijos.Contains(hijo));
        }


        // ================================================================
        // 5. AGREGAR PAREJA
        // ================================================================
        [TestMethod]
        public void TestAgregarNodo_Pareja()
        {
            var a = new MiembroFamilia("Luis", "3", DateTime.Now.AddYears(-40), true);
            var b = new MiembroFamilia("Ana", "4", DateTime.Now.AddYears(-38), true);

            bool resultado = ArbolGenealogicoService.AgregarNodo(null, a, b, 4);

            Assert.IsTrue(resultado);
            Assert.AreEqual(b, a.Pareja);
            Assert.AreEqual(a, b.Pareja);
        }


        // ================================================================
        // 6. AGREGAR HERMANO
        // ================================================================
        [TestMethod]
        public void TestAgregarNodo_Hermano()
        {
            var a = new MiembroFamilia("Pedro", "2", DateTime.Now.AddYears(-10), true);
            var b = new MiembroFamilia("Juan", "5", DateTime.Now.AddYears(-12), true);

            bool resultado = ArbolGenealogicoService.AgregarNodo(null, a, b, 5);

            Assert.IsTrue(resultado);
            Assert.IsTrue(a.Hermanos.Contains(b));
            Assert.IsTrue(b.Hermanos.Contains(a));
        }


        // ================================================================
        // 7. LIGAR TODO — Padres deben ser pareja
        // ================================================================
        [TestMethod]
        public void TestLigarTodo_PadresComoPareja()
        {
            var padre = new MiembroFamilia("Luis", "3", DateTime.Now.AddYears(-40), true);
            var madre = new MiembroFamilia("Ana", "4", DateTime.Now.AddYears(-38), true);
            var hijo = new MiembroFamilia("Pedro", "2", DateTime.Now.AddYears(-10), true);

            hijo.AsignarPadre(padre);
            hijo.AsignarMadre(madre);

            ArbolGenealogicoService.LigarTodo(hijo);

            Assert.AreEqual(padre.Pareja, madre);
            Assert.AreEqual(madre.Pareja, padre);
        }


        // ================================================================
        // 8. GENERAR MATRIZ (relaciones padre-hijo)
        // ================================================================
        [TestMethod]
        public void TestGenerarMatriz_PadreHijo()
        {
            var padre = new MiembroFamilia("Luis", "1", DateTime.Now.AddYears(-40), true);
            var hijo = new MiembroFamilia("Pedro", "2", DateTime.Now.AddYears(-10), true);

            hijo.AsignarPadre(padre);

            var lista = new List<MiembroFamilia> { padre, hijo };
            var matriz = ArbolGenealogicoService.GenerarMatriz(lista);

            Assert.AreEqual(0, matriz[0, 1]);  // padre → hijo
            Assert.AreEqual(2, matriz[1, 0]);  // hijo → padre
        }


        // ================================================================
        // 9. LAYOUT BÁSICO (padre debe estar arriba)
        // ================================================================
        [TestMethod]
        public void TestCalcularLayout_PadreArribaDeHijo()
        {
            var padre = new MiembroFamilia("Luis", "1", DateTime.Now.AddYears(-40), true);
            var hijo = new MiembroFamilia("Pedro", "2", DateTime.Now.AddYears(-10), true);

            hijo.AsignarPadre(padre);

            var lista = new List<MiembroFamilia> { padre, hijo };

            var layout = ArbolGenealogicoService.CalcularLayoutCompleto(lista);

            Assert.IsTrue(layout[padre.Cedula].Row < layout[hijo.Cedula].Row);
        }


        // ================================================================
        // 10. PRUEBA COMPLEJA: HERMANOS + PAREJA + HIJOS
        // ================================================================
        [TestMethod]
        public void TestRelacionCompleja_LigarTodo()
        {
            var padre = new MiembroFamilia("Carlos", "10", DateTime.Now.AddYears(-45), true);
            var madre = new MiembroFamilia("María", "11", DateTime.Now.AddYears(-43), true);

            var hijo1 = new MiembroFamilia("Pedro", "12", DateTime.Now.AddYears(-20), true);
            var hijo2 = new MiembroFamilia("Juan", "13", DateTime.Now.AddYears(-18), true);

            hijo1.AsignarPadre(padre);
            hijo1.AsignarMadre(madre);

            hijo2.AsignarPadre(padre);
            hijo2.AsignarMadre(madre);

            ArbolGenealogicoService.LigarTodo(hijo1);
            ArbolGenealogicoService.LigarTodo(hijo2);

            Assert.AreEqual(padre.Pareja, madre);
            Assert.IsTrue(hijo1.Hermanos.Contains(hijo2));
            Assert.IsTrue(hijo2.Hermanos.Contains(hijo1));
            Assert.IsTrue(padre.Hijos.Contains(hijo1));
            Assert.IsTrue(padre.Hijos.Contains(hijo2));
        }
    }
}
