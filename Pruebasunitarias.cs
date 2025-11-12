using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using ArbolGenealogico.Modelos;

namespace ArbolGenealogico.Tests
{
    [TestClass]
    public class ArbolGenealogicoTests
    {
        [TestMethod]
        public void TestAgregarHijo_AsignaPadreCorrectamente()
        {
            // Arrange
            var padre = new MiembroFamilia("Carlos", "1", new DateTime(1980, 5, 20), true);
            var hijo = new MiembroFamilia("Pedro", "2", new DateTime(2010, 3, 15), true);

            // Act
            padre.AgregarHijo(hijo);

            // Assert
            Assert.AreEqual(padre, hijo.Padre);
            Assert.IsTrue(padre.Hijos.Contains(hijo));
        }

        [TestMethod]
        public void TestAgregarHijo_NoPermitePadreDuplicado()
        {
            // Arrange
            var padre1 = new MiembroFamilia("Juan", "1", new DateTime(1970, 4, 10), true);
            var padre2 = new MiembroFamilia("Luis", "2", new DateTime(1972, 6, 22), true);
            var hijo = new MiembroFamilia("Andrés", "3", new DateTime(2000, 2, 10), true);

            // Act
            padre1.AgregarHijo(hijo);

            // Assert
            Assert.ThrowsException<InvalidOperationException>(() => padre2.AgregarHijo(hijo));
        }

        [TestMethod]
        public void TestBuscarPorCedula_EncuentraMiembro()
        {
            // Arrange
            var abuelo = new MiembroFamilia("José", "1", new DateTime(1950, 1, 1), false);
            var padre = new MiembroFamilia("Mario", "2", new DateTime(1975, 1, 1), true);
            var hijo = new MiembroFamilia("Luis", "3", new DateTime(2000, 1, 1), true);

            abuelo.AgregarHijo(padre);
            padre.AgregarHijo(hijo);

            var arbol = new ArbolGenealogico(abuelo);

            // Act
            var resultado = arbol.BuscarPorCedula(abuelo, "3");

            // Assert
            Assert.IsNotNull(resultado);
            Assert.AreEqual("Luis", resultado.Nombre);
        }

        [TestMethod]
        public void TestDetectarCiclo_DetectaCuandoExiste()
        {
            // Arrange
            var a = new MiembroFamilia("A", "1", DateTime.Now.AddYears(-60), true);
            var b = new MiembroFamilia("B", "2", DateTime.Now.AddYears(-30), true);
            var c = new MiembroFamilia("C", "3", DateTime.Now.AddYears(-10), true);

            a.AgregarHijo(b);
            b.AgregarHijo(c);

            // Forzamos un ciclo manualmente
            c.AgregarHijo(a);

            var arbol = new ArbolGenealogico(a);

            // Assert
            Assert.IsTrue(arbol.DetectarCiclo(a, new HashSet<string>()));
        }

        [TestMethod]
        public void TestAgregarHijo_ThrowsExceptionSiEsNulo()
        {
            // Arrange
            var padre = new MiembroFamilia("Luis", "1", new DateTime(1980, 1, 1), true);

            // Assert
            Assert.ThrowsException<ArgumentNullException>(() => padre.AgregarHijo(null));
        }
    }
}

