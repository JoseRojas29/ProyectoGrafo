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


    }
}
